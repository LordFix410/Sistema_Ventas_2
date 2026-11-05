# Librería Digital

Aplicación web local hecha con ASP.NET Core 5.0.

## Ejecutar

Desde esta carpeta, ejecutá `dotnet run`. La aplicación escucha en `http://0.0.0.0:5170` para aceptar conexiones de la red local. `global.json` fija el SDK 5.0.408 para este proyecto.

## Publicar y arrancar en Windows

El archivo `IniciarLibreriaDigital.bat` ejecuta `dotnet run` desde la carpeta del proyecto, por lo que inicia la versión actual. Para exponerla mediante el túnel, usa `IniciarVentasCloudflare.ps1` o `IniciarVentasAutomatico.bat`; el lanzador compila y publica el proyecto actual en `publicado` antes de levantarlo.

En la otra computadora, usa `http://IP-DEL-SERVIDOR:5170`.

## Iniciar con el túnel temporal de Cloudflare

El script `IniciarVentasCloudflare.ps1` compila el proyecto desde esta carpeta usando el SDK de `global.json`, inicia esa publicación, crea un Quick Tunnel y actualiza la clave `url` del KV de ventas. Si el puerto 5170 ya está ocupado por un proceso desconocido, se detiene para evitar usar una versión anterior.

1. Coloca una copia del API Token de Cloudflare en `cloudflare-token.txt`, junto a este README. También se acepta el nombre `cloudflare-token` sin extensión. El token debe tener únicamente permiso de escritura en Workers KV para la cuenta correspondiente.
2. Ejecuta `IniciarVentasAutomatico.bat` para iniciar en segundo plano, sin dejar abierta una ventana de consola. También puedes abrir directamente `IniciarVentasOculto.vbs`.
3. Para apagarlo, ejecuta `DetenerVentas.bat`. El lanzador evita iniciar una segunda copia y guarda errores en `logs\lanzador.log`; los registros propios de la app y de Cloudflare quedan también en `logs`.

### Iniciar automáticamente al entrar a Windows

1. Pulsa `Windows + R`, escribe `shell:startup` y presiona Enter.
2. En la carpeta que se abre, crea un acceso directo a `IniciarVentasOculto.vbs`.
3. La próxima vez que inicies sesión en Windows, el lanzador iniciará la aplicación, creará un túnel y publicará su nueva URL en el KV.

El token local y los registros se excluyen de Git. No subas ni compartas el token. El Quick Tunnel genera una dirección temporal; para uso continuo o productivo, configura un túnel con nombre y hostname estable.

## Registro de productos

La página `productos.html` busca la cuenta activa de `dbo.USUARIO` por contraseña. Para que una contraseña identifique a una sola persona, cada cuenta debe tener una clave distinta. Antes de iniciarla, configura la conexión SQL Server **solo en el servidor**, como variable de entorno `ConnectionStrings__ProductDb`. No pongas credenciales de SQL en `wwwroot`, JavaScript ni archivos publicados. La sesión usa una cookie `HttpOnly`, `SameSite=Strict` y `Secure`; accede a esta página mediante el túnel HTTPS.

El formulario crea productos mediante `dbo.sp_RegistrarProducto`. El stock queda en cero; no registra entradas de inventario. La navegación incluye mantenimiento de categorías, grupos de productos y grupos de proveedores. Los grupos de proveedor se asocian a proveedores ya registrados. Los límites mínimo y máximo se comparten por grupo de productos; los productos sin grupo guardan sus límites individuales.

Si la base se creó antes de esta página y al guardar aparece el error SQL 8144, ejecutá una vez `migracion_sp_registrar_producto.sql` sobre `DBSISTEMA_VENTA`. Actualiza solo el procedimiento de registro existente y conserva los datos.

El sitio está en `wwwroot`. Los estilos y scripts están separados en `wwwroot/css` y `wwwroot/js`. La API local recibe archivos ZIP en `POST /api/images/unpack-zip`.

## Venta desde el teléfono

Abre `https://TU-SERVIDOR/venta.html` desde el teléfono. Inicia sesión una vez con la contraseña del vendedor; la sesión autenticada se conserva en el servidor hasta 8 horas de inactividad y se comparte entre las páginas de productos y ventas. No se guarda la contraseña en el teléfono.

La página permite buscar productos por código o nombre, escanearlos con la cámara, sumar escaneos repetidos, editar cantidades o quitar renglones, agregar artículos temporales, consultar precios aparte y buscar clientes por NIT. Si el cliente no existe, se puede crear desde el diálogo sin perder el carrito. La venta guarda el NIT y nombre seleccionados; si no se elige un cliente, queda como CF. El tipo de documento sigue siendo boleta: todavía no genera factura fiscal. El servidor vuelve a leer los precios vigentes, calcula total y vuelto, identifica al vendedor de la sesión y registra mediante `dbo.usp_RegistrarVenta` y `dbo.EDetalle_Venta` en la misma base central.

El escaneo requiere HTTPS, permiso de cámara y un navegador móvil que implemente `BarcodeDetector`. Si el navegador no lo admite, se puede seguir buscando por código o nombre. La conexión SQL permanece únicamente en el servidor y se configura con `ConnectionStrings__ProductDb`.

## Fotografías de productos

Al crear un producto desde `productos.html`, se puede tomar o elegir una foto JPG/PNG de hasta 5 MB. El servidor la guarda en `DOTNET/imagenes/productos` (o en la ruta indicada por `ProductImages__Path`) y la publica bajo `/imagenes/productos/...`. La carpeta se crea al iniciar el servidor y se mantiene fuera de `publicado`, para que una nueva publicación no borre las fotografías. Windows Forms manda las fotos mediante `POST /api/productos/imagenes` y las descarga desde `/imagenes/...`; el resto de los datos del producto sigue guardándose directamente desde la aplicación en SQL Server. Forms determina el host del servicio web usando el servidor configurado en `conexion.txt`, que debe ser el mismo equipo que aloja ASP.NET. No hay carpeta compartida ni `imagenes_productos.txt`. Incluye `ImagenRuta` y el parámetro correspondiente de `dbo.sp_RegistrarProducto` de la migración de fotografía.

En la consulta de precios, cada producto escaneado se conserva en una lista hasta quitarlo; volver a escanearlo aumenta su cantidad. El navegador muestra la foto si existe, el nombre, precio, cantidad y la opción para quitar.
