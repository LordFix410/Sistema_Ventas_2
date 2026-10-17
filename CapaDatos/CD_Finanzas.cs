using System;
using System.Data;
using System.Data.SqlClient;

namespace CapaDatos
{
    /// <summary>Acceso a consultas y operaciones del panel financiero.</summary>
    public class CD_Finanzas
    {
        public DataSet ObtenerPanel(DateTime desde, DateTime hasta)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.cadena))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand crearPeriodo = new SqlCommand(@"
INSERT dbo.GASTO_PERSONAL_PERIODO(IdGastoPersonal,Anio,Mes,MontoProgramado,FechaVencimiento)
SELECT g.IdGastoPersonal,YEAR(GETDATE()),MONTH(GETDATE()),g.MontoMensual,
       CASE WHEN g.DiaVencimiento IS NULL THEN NULL
            ELSE DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),
                 CASE WHEN g.DiaVencimiento > DAY(EOMONTH(GETDATE())) THEN DAY(EOMONTH(GETDATE())) ELSE g.DiaVencimiento END) END
FROM dbo.GASTO_PERSONAL g
WHERE g.Activo=1 AND NOT EXISTS
  (SELECT 1 FROM dbo.GASTO_PERSONAL_PERIODO p WHERE p.IdGastoPersonal=g.IdGastoPersonal AND p.Anio=YEAR(GETDATE()) AND p.Mes=MONTH(GETDATE()));", cn, tx))
                        {
                            crearPeriodo.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }

                string sql = @"
SELECT c.IdCompra,pr.RazonSocial AS Proveedor,c.NumeroDocumento,
       DATEADD(day,30,c.FechaRegistro) AS FechaVencimiento,
       CAST(c.MontoTotal-ISNULL(p.Pagado,0) AS decimal(12,2)) AS SaldoPendiente,
       DATEDIFF(day,CONVERT(date,GETDATE()),CONVERT(date,DATEADD(day,30,c.FechaRegistro))) AS DiasRestantes
FROM dbo.COMPRA c JOIN dbo.PROVEEDOR pr ON pr.IdProveedor=c.IdProveedor
OUTER APPLY(SELECT SUM(m.Monto) Pagado FROM dbo.FIN_MOVIMIENTO m WHERE m.IdCompra=c.IdCompra AND m.Tipo='Proveedor') p
WHERE c.MontoTotal>ISNULL(p.Pagado,0)
ORDER BY DATEADD(day,30,c.FechaRegistro),pr.RazonSocial;

SELECT CAST(ISNULL((SELECT SUM(MontoMensual) FROM dbo.GASTO_PERSONAL WHERE Activo=1),0) AS decimal(12,2)) GastosMensuales,
       CAST(ISNULL((SELECT SUM(MontoTotal) FROM dbo.VENTA WHERE FechaRegistro>=CONVERT(date,GETDATE()) AND FechaRegistro<DATEADD(day,1,CONVERT(date,GETDATE()))),0) AS decimal(12,2)) VentaHoy;

SELECT CAST(ISNULL((SELECT SUM(CapitalGenerado) FROM dbo.FIN_CIERRE_CAJA),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE Tipo IN ('Empresa','Proveedor')),0) AS decimal(12,2)) CapitalDisponible,
       CAST(ISNULL((SELECT SUM(GananciaGenerada) FROM dbo.FIN_CIERRE_CAJA),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL),0) AS decimal(12,2)) GananciaAcumulada,
       CAST(ISNULL((SELECT SUM(c.MontoTotal) FROM dbo.COMPRA c),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE Tipo='Proveedor'),0) AS decimal(12,2)) DeudaProveedores,
       CAST(ISNULL((SELECT SUM(p.MontoProgramado) FROM dbo.GASTO_PERSONAL_PERIODO p),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL),0) AS decimal(12,2)) DeudaPersonal;

SELECT c.IdCompra,pr.RazonSocial+' - '+c.NumeroDocumento+' (Q '+FORMAT(c.MontoTotal-ISNULL(x.Pagado,0),'N2')+')' AS Etiqueta,
       CAST(c.MontoTotal-ISNULL(x.Pagado,0) AS decimal(12,2)) Saldo
FROM dbo.COMPRA c JOIN dbo.PROVEEDOR pr ON pr.IdProveedor=c.IdProveedor
OUTER APPLY(SELECT SUM(Monto) Pagado FROM dbo.FIN_MOVIMIENTO m WHERE m.IdCompra=c.IdCompra AND m.Tipo='Proveedor') x
WHERE c.MontoTotal>ISNULL(x.Pagado,0)
ORDER BY c.FechaRegistro;

SELECT p.IdPeriodoGastoPersonal,p.IdGastoPersonal,g.Nombre+' - Q '+FORMAT(p.MontoProgramado-ISNULL(x.Pagado,0),'N2') AS Etiqueta,
       CAST(p.MontoProgramado-ISNULL(x.Pagado,0) AS decimal(12,2)) Saldo
FROM dbo.GASTO_PERSONAL_PERIODO p JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=p.IdGastoPersonal
OUTER APPLY(SELECT SUM(Monto) Pagado FROM dbo.PAGO_GASTO_PERSONAL q WHERE q.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal) x
WHERE p.Anio=YEAR(GETDATE()) AND p.Mes=MONTH(GETDATE()) AND p.MontoProgramado>ISNULL(x.Pagado,0) AND g.Activo=1;

SELECT TOP 300 Fecha,Tipo,Concepto,Factura,Monto,Responsable FROM (
    SELECT m.Fecha,m.Tipo,m.Concepto,ISNULL(c.NumeroDocumento,'') Factura,m.Monto,u.NombreCompleto Responsable
    FROM dbo.FIN_MOVIMIENTO m LEFT JOIN dbo.COMPRA c ON c.IdCompra=m.IdCompra LEFT JOIN dbo.USUARIO u ON u.IdUsuario=m.IdUsuario
    UNION ALL
    SELECT p.FechaPago,'Personal',g.Nombre,'',p.Monto,u.NombreCompleto
    FROM dbo.PAGO_GASTO_PERSONAL p JOIN dbo.GASTO_PERSONAL_PERIODO per ON per.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal
    JOIN dbo.GASTO_PERSONAL g ON g.IdGastoPersonal=per.IdGastoPersonal LEFT JOIN dbo.USUARIO u ON u.IdUsuario=p.IdUsuario
) m WHERE Fecha>=@desde AND Fecha<@hasta ORDER BY Fecha DESC;

SELECT TOP 1 IdCierre,FechaCierre,EfectivoContado,CapitalGenerado,GananciaGenerada
FROM dbo.FIN_CIERRE_CAJA ORDER BY FechaCierre DESC;

SELECT c.IdCierre,
       'Cierre '+CONVERT(varchar(16),c.FechaCierre,120)+' | Capital Q '+FORMAT(c.CapitalGenerado-ISNULL(d.Capital,0),'N2')+' | Ganancia Q '+FORMAT(c.GananciaGenerada-ISNULL(d.Ganancia,0),'N2') AS Etiqueta,
       CAST(c.CapitalGenerado-ISNULL(d.Capital,0) AS decimal(12,2)) CapitalPendiente,
       CAST(c.GananciaGenerada-ISNULL(d.Ganancia,0) AS decimal(12,2)) GananciaPendiente
FROM dbo.FIN_CIERRE_CAJA c
OUTER APPLY(SELECT SUM(Capital) Capital,SUM(Ganancia) Ganancia FROM dbo.FIN_DEPOSITO WHERE IdCierre=c.IdCierre) d
WHERE c.CapitalGenerado-ISNULL(d.Capital,0)>0 OR c.GananciaGenerada-ISNULL(d.Ganancia,0)>0
ORDER BY c.FechaCierre;";
                using (SqlDataAdapter da = new SqlDataAdapter(sql, cn))
                {
                    da.SelectCommand.Parameters.Add("@desde", SqlDbType.DateTime2).Value = desde;
                    da.SelectCommand.Parameters.Add("@hasta", SqlDbType.DateTime2).Value = hasta;
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            }
        }

        public DataTable ObtenerCifrasCierre(DateTime inicioSolicitado, decimal fondoInicial)
        {
            const string sql = @"
DECLARE @inicio datetime2(0)=@inicioSolicitado, @fondo decimal(12,2)=@fondoInicial;
IF EXISTS(SELECT 1 FROM dbo.FIN_CIERRE_CAJA)
BEGIN
    SELECT TOP 1 @inicio=FechaCierre,@fondo=EfectivoContado FROM dbo.FIN_CIERRE_CAJA ORDER BY FechaCierre DESC;
END;
SELECT @inicio FechaInicio,@fondo FondoInicial,
       ISNULL(v.Ventas,0) Ventas,ISNULL(v.Costo,0) Costo,
       ISNULL(v.Ganancia,0) Ganancia,
       CAST(CASE WHEN ISNULL(v.Ganancia,0)>0 THEN v.Ganancia*0.15 ELSE 0 END AS decimal(12,2)) ReservaCapital,
       CAST(ISNULL(v.Costo,0)+CASE WHEN ISNULL(v.Ganancia,0)>0 THEN ISNULL(v.Ganancia,0)*0.15 ELSE 0 END AS decimal(12,2)) CapitalADepositar,
       CAST(CASE WHEN ISNULL(v.Ganancia,0)>0 THEN ISNULL(v.Ganancia,0)*0.85 ELSE ISNULL(v.Ganancia,0) END AS decimal(12,2)) GananciaADepositar,
       CAST(@fondo+ISNULL(v.Ventas,0)-ISNULL(m.Salidas,0)-ISNULL(d.Depositos,0) AS decimal(12,2)) EfectivoEsperado
FROM (SELECT (SELECT SUM(MontoTotal) FROM dbo.VENTA WHERE FechaRegistro>=@inicio AND FechaRegistro<=GETDATE()) Ventas,
             (SELECT SUM(dc.PrecioCompra*dc.Cantidad) FROM dbo.DETALLE_VENTA dc JOIN dbo.VENTA x ON x.IdVenta=dc.IdVenta WHERE x.FechaRegistro>=@inicio AND x.FechaRegistro<=GETDATE()) Costo,
             (SELECT SUM((dc.PrecioVenta-dc.PrecioCompra)*dc.Cantidad) FROM dbo.DETALLE_VENTA dc JOIN dbo.VENTA x ON x.IdVenta=dc.IdVenta WHERE x.FechaRegistro>=@inicio AND x.FechaRegistro<=GETDATE()) Ganancia) v
CROSS APPLY(SELECT ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE MedioPago='Efectivo' AND Fecha>=@inicio AND Fecha<=GETDATE()),0)+
                   ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE MedioPago='Efectivo' AND FechaPago>=@inicio AND FechaPago<=GETDATE()),0) Salidas) m
CROSS APPLY(SELECT ISNULL((SELECT SUM(Capital+Ganancia) FROM dbo.FIN_DEPOSITO WHERE Fecha>=@inicio AND Fecha<=GETDATE()),0) Depositos) d;";
            using (SqlConnection cn = new SqlConnection(Conexion.cadena))
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@inicioSolicitado", SqlDbType.DateTime2).Value = inicioSolicitado;
                cmd.Parameters.Add("@fondoInicial", SqlDbType.Decimal).Value = fondoInicial;
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                return dt;
            }
        }

        public bool RegistrarMovimiento(string tipo, int? idRelacionado, int idUsuario, DateTime fecha,
            string concepto, decimal monto, string medioPago, string referencia, out string mensaje)
        {
            mensaje = string.Empty;
            using (SqlConnection cn = new SqlConnection(Conexion.cadena))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        if (tipo == "Personal")
                        {
                            const string sqlPersonal = @"
DECLARE @saldo decimal(12,2);
SELECT @saldo=p.MontoProgramado-ISNULL(SUM(pg.Monto),0)
FROM dbo.GASTO_PERSONAL_PERIODO p WITH(UPDLOCK,HOLDLOCK)
LEFT JOIN dbo.PAGO_GASTO_PERSONAL pg ON pg.IdPeriodoGastoPersonal=p.IdPeriodoGastoPersonal
WHERE p.IdPeriodoGastoPersonal=@id GROUP BY p.MontoProgramado;
IF @saldo IS NULL OR @monto>@saldo THROW 51001,'El monto supera el saldo pendiente de este gasto personal.',1;
INSERT dbo.PAGO_GASTO_PERSONAL(IdPeriodoGastoPersonal,IdUsuario,Monto,FechaPago,MedioPago,Referencia,Observaciones)
VALUES(@id,@usuario,@monto,@fecha,@medio,@referencia,@concepto);";
                            using (SqlCommand cmd = new SqlCommand(sqlPersonal, cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@id", idRelacionado.Value);
                                AgregarParametrosMovimiento(cmd, idUsuario, fecha, concepto, monto, medioPago, referencia);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            if (tipo == "Proveedor")
                            {
                                using (SqlCommand validar = new SqlCommand(@"IF @monto > (SELECT c.MontoTotal-ISNULL(SUM(m.Monto),0) FROM dbo.COMPRA c WITH(UPDLOCK,HOLDLOCK) LEFT JOIN dbo.FIN_MOVIMIENTO m ON m.IdCompra=c.IdCompra AND m.Tipo='Proveedor' WHERE c.IdCompra=@id GROUP BY c.MontoTotal) THROW 51002,'El pago supera el saldo pendiente de la factura.',1;", cn, tx))
                                {
                                    validar.Parameters.AddWithValue("@id", idRelacionado.Value);
                                    validar.Parameters.AddWithValue("@monto", monto);
                                    validar.ExecuteNonQuery();
                                }
                            }
                            using (SqlCommand cmd = new SqlCommand(@"INSERT dbo.FIN_MOVIMIENTO(Tipo,IdCompra,IdUsuario,Fecha,Concepto,Monto,MedioPago,Referencia) VALUES(@tipo,@compra,@usuario,@fecha,@concepto,@monto,@medio,@referencia)", cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@tipo", tipo == "Proveedor" ? "Proveedor" : "Empresa");
                                cmd.Parameters.AddWithValue("@compra", idRelacionado.HasValue && tipo == "Proveedor" ? (object)idRelacionado.Value : DBNull.Value);
                                AgregarParametrosMovimiento(cmd, idUsuario, fecha, concepto, monto, medioPago, referencia);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback(); mensaje = ex.Message; return false;
                    }
                }
            }
        }

        private void AgregarParametrosMovimiento(SqlCommand cmd, int idUsuario, DateTime fecha,
            string concepto, decimal monto, string medioPago, string referencia)
        {
            cmd.Parameters.AddWithValue("@usuario", idUsuario);
            cmd.Parameters.AddWithValue("@fecha", fecha);
            cmd.Parameters.AddWithValue("@concepto", concepto);
            cmd.Parameters.AddWithValue("@monto", monto);
            cmd.Parameters.AddWithValue("@medio", medioPago);
            cmd.Parameters.AddWithValue("@referencia", string.IsNullOrWhiteSpace(referencia) ? (object)DBNull.Value : referencia);
        }

        public bool RegistrarCierre(int idUsuario, DateTime inicioSolicitado, decimal fondoInicial,
            decimal efectivoContado, out string mensaje, out int idCierre)
        {
            mensaje = string.Empty; idCierre = 0;
            using (SqlConnection cn = new SqlConnection(Conexion.cadena))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        DateTime ahora = DateTime.Now;
                        DataTable calculo;
                        using (SqlCommand cmd = new SqlCommand(@"
DECLARE @inicio datetime2(0)=@solicitado,@fondo decimal(12,2)=@fondoUi;
IF EXISTS(SELECT 1 FROM dbo.FIN_CIERRE_CAJA WITH(UPDLOCK,HOLDLOCK))
    SELECT TOP 1 @inicio=FechaCierre,@fondo=EfectivoContado FROM dbo.FIN_CIERRE_CAJA ORDER BY FechaCierre DESC;
IF @inicio>=@ahora THROW 51003,'La hora del cierre debe ser posterior al cierre anterior.',1;
DECLARE @ventas decimal(12,2),@costo decimal(12,2),@ganancia decimal(12,2),@salidas decimal(12,2),@depositos decimal(12,2),@esperado decimal(12,2);
SELECT @ventas=ISNULL((SELECT SUM(MontoTotal) FROM dbo.VENTA WHERE FechaRegistro>=@inicio AND FechaRegistro<=@ahora),0),
       @costo=ISNULL((SELECT SUM(d.PrecioCompra*d.Cantidad) FROM dbo.DETALLE_VENTA d JOIN dbo.VENTA v ON v.IdVenta=d.IdVenta WHERE v.FechaRegistro>=@inicio AND v.FechaRegistro<=@ahora),0),
       @ganancia=ISNULL((SELECT SUM((d.PrecioVenta-d.PrecioCompra)*d.Cantidad) FROM dbo.DETALLE_VENTA d JOIN dbo.VENTA v ON v.IdVenta=d.IdVenta WHERE v.FechaRegistro>=@inicio AND v.FechaRegistro<=@ahora),0);
SELECT @salidas=ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO WHERE MedioPago='Efectivo' AND Fecha>=@inicio AND Fecha<=@ahora),0)+ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL WHERE MedioPago='Efectivo' AND FechaPago>=@inicio AND FechaPago<=@ahora),0);
SELECT @depositos=ISNULL(SUM(Capital+Ganancia),0) FROM dbo.FIN_DEPOSITO WHERE Fecha>=@inicio AND Fecha<=@ahora;
SET @esperado=@fondo+@ventas-@salidas-@depositos;
SELECT @inicio FechaInicio,@fondo FondoInicial,@ventas Ventas,@costo Costo,@ganancia Ganancia,@esperado Esperado;", cn, tx))
                        {
                            cmd.Parameters.Add("@solicitado", SqlDbType.DateTime2).Value = inicioSolicitado;
                            cmd.Parameters.Add("@fondoUi", SqlDbType.Decimal).Value = fondoInicial;
                            cmd.Parameters.Add("@ahora", SqlDbType.DateTime2).Value = ahora;
                            calculo = new DataTable(); using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(calculo);
                        }
                        DataRow r = calculo.Rows[0];
                        decimal ventas = Convert.ToDecimal(r["Ventas"]), costo = Convert.ToDecimal(r["Costo"]), ganancia = Convert.ToDecimal(r["Ganancia"]);
                        decimal capitalBase = costo + (ganancia > 0 ? Math.Round(ganancia * 0.15m, 2) : 0m);
                        decimal gananciaBase = ganancia > 0 ? Math.Round(ganancia * 0.85m, 2) : ganancia;
                        decimal esperado = Convert.ToDecimal(r["Esperado"]), diferencia = efectivoContado - esperado;
                        decimal descGanancia = 0, descCapital = 0, sobrante = 0;
                        if (diferencia < 0)
                        {
                            decimal faltante = Math.Abs(diferencia);
                            decimal saldoGanancia = SaldoDisponible(cn, tx, true) + gananciaBase;
                            descGanancia = Math.Min(faltante, Math.Max(0, saldoGanancia));
                            faltante -= descGanancia;
                            decimal saldoCapital = SaldoDisponible(cn, tx, false) + capitalBase;
                            descCapital = Math.Min(faltante, Math.Max(0, saldoCapital));
                            faltante -= descCapital;
                            if (faltante > 0) descCapital += faltante;
                        }
                        else if (diferencia > 0) sobrante = diferencia;

                        using (SqlCommand ins = new SqlCommand(@"INSERT dbo.FIN_CIERRE_CAJA(IdUsuario,FechaInicio,FechaCierre,FondoInicial,Ventas,CostoProductos,GananciaBruta,ReservaCapital,CapitalGenerado,GananciaGenerada,EfectivoEsperado,EfectivoContado,Diferencia,DescuentoGanancia,DescuentoCapital,Sobrante)
VALUES(@usuario,@inicio,@ahora,@fondo,@ventas,@costo,@ganancia,@reserva,@capital,@gananciaNeta,@esperado,@contado,@dif,@descGan,@descCap,@sobrante); SELECT CONVERT(int,SCOPE_IDENTITY());", cn, tx))
                        {
                            ins.Parameters.AddWithValue("@usuario", idUsuario);
                            ins.Parameters.AddWithValue("@inicio", r["FechaInicio"]);
                            ins.Parameters.AddWithValue("@ahora", ahora);
                            ins.Parameters.AddWithValue("@fondo", r["FondoInicial"]);
                            ins.Parameters.AddWithValue("@ventas", ventas);
                            ins.Parameters.AddWithValue("@costo", costo);
                            ins.Parameters.AddWithValue("@ganancia", ganancia);
                            ins.Parameters.AddWithValue("@reserva", ganancia > 0 ? Math.Round(ganancia * 0.15m, 2) : 0m);
                            ins.Parameters.AddWithValue("@capital", capitalBase - descCapital);
                            ins.Parameters.AddWithValue("@gananciaNeta", gananciaBase - descGanancia);
                            ins.Parameters.AddWithValue("@esperado", esperado);
                            ins.Parameters.AddWithValue("@contado", efectivoContado);
                            ins.Parameters.AddWithValue("@dif", diferencia);
                            ins.Parameters.AddWithValue("@descGan", descGanancia);
                            ins.Parameters.AddWithValue("@descCap", descCapital);
                            ins.Parameters.AddWithValue("@sobrante", sobrante);
                            idCierre = Convert.ToInt32(ins.ExecuteScalar());
                        }
                        tx.Commit(); return true;
                    }
                    catch (Exception ex) { tx.Rollback(); mensaje = ex.Message; return false; }
                }
            }
        }

        private decimal SaldoDisponible(SqlConnection cn, SqlTransaction tx, bool ganancia)
        {
            string sql = ganancia
                ? "SELECT ISNULL(SUM(GananciaGenerada),0)-ISNULL((SELECT SUM(Monto) FROM dbo.PAGO_GASTO_PERSONAL),0) FROM dbo.FIN_CIERRE_CAJA"
                : "SELECT ISNULL(SUM(CapitalGenerado),0)-ISNULL((SELECT SUM(Monto) FROM dbo.FIN_MOVIMIENTO),0) FROM dbo.FIN_CIERRE_CAJA";
            using (SqlCommand cmd = new SqlCommand(sql, cn, tx)) return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        public bool RegistrarDeposito(int idCierre, int idUsuario, decimal capital, decimal ganancia, string referencia, out string mensaje)
        {
            mensaje = string.Empty;
            if (capital < 0 || ganancia < 0 || capital + ganancia <= 0) { mensaje = "Ingresa un monto depositado mayor que cero."; return false; }
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.cadena))
                using (SqlCommand cmd = new SqlCommand(@"
DECLARE @capitalMax decimal(12,2),@gananciaMax decimal(12,2);
SELECT @capitalMax=CapitalGenerado,@gananciaMax=GananciaGenerada FROM dbo.FIN_CIERRE_CAJA WHERE IdCierre=@cierre;
IF @capitalMax IS NULL THROW 51004,'No existe el cierre seleccionado.',1;
IF @capital+ISNULL((SELECT SUM(Capital) FROM dbo.FIN_DEPOSITO WHERE IdCierre=@cierre),0)>@capitalMax
    THROW 51005,'El depósito acumulado de capital supera el monto asignado en el cierre.',1;
IF @ganancia+ISNULL((SELECT SUM(Ganancia) FROM dbo.FIN_DEPOSITO WHERE IdCierre=@cierre),0)>@gananciaMax
    THROW 51006,'El depósito acumulado de ganancia supera el monto asignado en el cierre.',1;
INSERT dbo.FIN_DEPOSITO(IdCierre,IdUsuario,Capital,Ganancia,Referencia) VALUES(@cierre,@usuario,@capital,@ganancia,@ref);", cn))
                {
                    cmd.Parameters.AddWithValue("@cierre", idCierre); cmd.Parameters.AddWithValue("@usuario", idUsuario); cmd.Parameters.AddWithValue("@capital", capital);
                    cmd.Parameters.AddWithValue("@ganancia", ganancia); cmd.Parameters.AddWithValue("@ref", string.IsNullOrWhiteSpace(referencia) ? (object)DBNull.Value : referencia);
                    cn.Open(); cmd.ExecuteNonQuery(); return true;
                }
            }
            catch (Exception ex) { mensaje = ex.Message; return false; }
        }
    }
}
