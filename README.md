# Sistema de Ventas

Aplicación de escritorio para administrar ventas, compras e inventario. Está desarrollada en C# con Windows Forms sobre .NET Framework 4.8 y usa SQL Server como base de datos central.

## Funciones

- Registro de ventas y compras con actualización de existencias.
- Catálogo de productos, categorías, clientes y proveedores.
- Artículos temporales para ventas desde la pantalla de venta con `F2`.
- Margen porcentual y precio de venta calculado con redondeo a múltiplos de 0.25.
- Reportes de ventas y compras con exportación.
- Usuarios y permisos por rol. Al confirmar una venta se valida la contraseña del vendedor activo que quedará asociado a esa venta.
- Conexión de varias computadoras cliente a una base SQL Server en la red local.

## Componentes

- `CapaPresentacion`: formularios Windows Forms y recursos visuales.
- `CapaNegocio`: validaciones y lógica del sistema.
- `CapaDatos`: acceso a SQL Server.
- `CapaEntidad`: modelos de datos.
- `query.sql`: script de instalación limpia de la base de datos.
- `PASOS_USUARIO_SQL_SERVER.txt`: configuración del usuario de SQL Server para la aplicación.
- `Instalador.iss`: fuente del instalador de Windows.

## Requisitos

- Windows con .NET Framework 4.8.
- SQL Server instalado en la computadora que alojará la base de datos.
- SQL Server Management Studio (SSMS) para ejecutar el script inicial.
- Visual Studio con soporte para .NET Framework 4.8, si se compila desde el código.
- Inno Setup 6.3 o posterior para generar el instalador desde `Instalador.iss`.

> SQL Server Developer se puede usar para desarrollo y pruebas internas, pero su licencia no permite usarlo como servidor de producción. Consulta la [documentación oficial de las ediciones de SQL Server](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-latest?view=sql-server-ver15).

## Instalación de la base de datos

Estos pasos se ejecutan **una sola vez en la computadora servidor**:

1. Abre SSMS y conéctate a la instancia de SQL Server que alojará la base.
2. Abre `query.sql` y ejecuta el archivo completo. Crea `DBSISTEMA_VENTA`, sus tablas, procedimientos y datos iniciales de ejemplo.
3. Sigue `PASOS_USUARIO_SQL_SERVER.txt` para habilitar la autenticación SQL y crear o actualizar el login de la aplicación.
4. Si se conectarán otras computadoras, habilita TCP/IP para la instancia y permite el puerto configurado (habitualmente `1433`) en el firewall de Windows para la red local.

**Importante:** `query.sql` es para una instalación limpia. Si `DBSISTEMA_VENTA` ya existe, no vuelvas a ejecutar el archivo completo sobre esa instancia: el `CREATE DATABASE` fallará. Haz una copia de seguridad antes de cualquier cambio de esquema.

El script incluye datos de ejemplo, entre ellos usuarios de prueba:

| Documento de inicio | Contraseña inicial | Rol |
| --- | --- | --- |
| `admin` | `123` | Administrador |
| `empleado1` | `1234` | Empleado |

**Cambia o desactiva estas cuentas antes de usar datos reales.** No reutilices la contraseña del login SQL como contraseña de un usuario del sistema.

## Compilar la aplicación

1. Abre `Sistema_Ventas.sln` en Visual Studio.
2. Restaura los paquetes NuGet del proyecto.
3. Selecciona la configuración **Release** y compila la solución.

La aplicación compilada queda en `CapaPresentacion/bin/Release/`.

Para ejecutar desde Visual Studio, copia `CapaPresentacion/conexion.ejemplo.txt` como `CapaPresentacion/conexion.txt` y configura la cadena con los datos de tu servidor. El archivo `conexion.txt` está excluido de Git porque contiene credenciales.

## Generar e instalar el instalador

1. Compila primero la solución en configuración **Release**.
2. Abre `Instalador.iss` con Inno Setup Compiler y compílalo.
3. El instalador se genera en `Instalador/Salida/`.
4. Durante la instalación, ingresa el nombre o IP del servidor, puerto, usuario SQL y contraseña. La contraseña se oculta al escribir.
5. El instalador crea `conexion.txt` junto al programa. No instala SQL Server ni crea la base de datos; la base debe estar preparada previamente en el servidor.

La cadena de conexión queda guardada como texto en `conexion.txt`. Protege el acceso a ese archivo y no lo compartas ni lo subas a GitHub. Para cambiar el servidor o la contraseña, vuelve a ejecutar el instalador e ingresa los datos nuevos, o edita el archivo junto al ejecutable.

## Conexión desde varias computadoras

Instala el cliente en cada computadora y durante cada instalación usa la IP o nombre de la computadora servidor y el puerto SQL correspondiente. Todas las instalaciones deben apuntar a la misma base `DBSISTEMA_VENTA`.

La cantidad práctica de equipos dependerá del servidor, la red y el uso simultáneo. No expongas el puerto de SQL Server directamente a Internet; limita el acceso a la red local.

## Archivos que no deben subirse

- `CapaPresentacion/conexion.txt` y cualquier archivo con cadenas o contraseñas reales.
- Copias de seguridad y archivos de datos de SQL Server (`.bak`, `.mdf`, `.ldf`, etc.).
- Archivos generados de compilación o el instalador generado; el código fuente del instalador sí es `Instalador.iss`.

El `.gitignore` del repositorio excluye la configuración local y los archivos de datos indicados.
