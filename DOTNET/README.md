# Librería Digital

Aplicación web local hecha con ASP.NET Core 5.0.

## Ejecutar

Desde esta carpeta, ejecutá `dotnet run`. La aplicación escucha en `http://0.0.0.0:5170` para aceptar conexiones de la red local.

## Publicar y arrancar en Windows

Publicá una vez desde esta carpeta con `dotnet publish -c Release -o publicado`. El archivo `IniciarLibreriaDigital.bat` ejecuta esa publicación en el puerto 5170. Para iniciarla al encender el equipo, crea una tarea en el Programador de tareas con desencadenador **Al iniciar el sistema** y acción **Iniciar un programa**, seleccionando el `.bat`.

En la otra computadora, usa `http://IP-DEL-SERVIDOR:5170`.

## Registro de productos

La página `productos.html` busca la cuenta activa de `dbo.USUARIO` por contraseña. Para que una contraseña identifique a una sola persona, cada cuenta debe tener una clave distinta. Antes de iniciarla, configura la conexión SQL Server **solo en el servidor**, como variable de entorno `ConnectionStrings__ProductDb`. No pongas credenciales de SQL en `wwwroot`, JavaScript ni archivos publicados. La sesión usa una cookie `HttpOnly`, `SameSite=Strict` y `Secure`; accede a esta página mediante el túnel HTTPS.

El formulario crea productos mediante `dbo.sp_RegistrarProducto`. El stock queda en cero; no registra entradas de inventario. La navegación incluye mantenimiento de categorías, grupos de productos y grupos de proveedores. Los grupos de proveedor se asocian a proveedores ya registrados. Los límites mínimo y máximo se comparten por grupo de productos; los productos sin grupo guardan sus límites individuales.

Si la base se creó antes de esta página y al guardar aparece el error SQL 8144, ejecutá una vez `migracion_sp_registrar_producto.sql` sobre `DBSISTEMA_VENTA`. Actualiza solo el procedimiento de registro existente y conserva los datos.

El sitio está en `wwwroot`. Los estilos y scripts están separados en `wwwroot/css` y `wwwroot/js`. La API local recibe archivos ZIP en `POST /api/images/unpack-zip`.

## Venta desde el teléfono

Abre `https://TU-SERVIDOR/venta.html` desde el teléfono. Inicia sesión una vez con la contraseña del vendedor; la sesión autenticada se conserva en el servidor hasta 8 horas de inactividad y se comparte entre las páginas de productos y ventas. No se guarda la contraseña en el teléfono.

La página permite buscar productos por código o nombre, escanearlos con la cámara, sumar escaneos repetidos, editar cantidades o quitar renglones, agregar artículos temporales, consultar precios aparte y buscar clientes por NIT. Si el cliente no existe, se puede crear desde el diálogo sin perder el carrito. La venta guarda el NIT y nombre seleccionados; si no se elige un cliente, queda como CF. El tipo de documento sigue siendo boleta: todavía no genera factura fiscal. El servidor vuelve a leer los precios vigentes, calcula total y vuelto, identifica al vendedor de la sesión y registra mediante `dbo.usp_RegistrarVenta` y `dbo.EDetalle_Venta` en la misma base central.

El escaneo requiere HTTPS, permiso de cámara y un navegador móvil que implemente `BarcodeDetector`. Si el navegador no lo admite, se puede seguir buscando por código o nombre. La conexión SQL permanece únicamente en el servidor y se configura con `ConnectionStrings__ProductDb`.
