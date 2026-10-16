#define MyAppName "Sistema de Ventas"
#define MyAppVersion "1.0.0"
#define MyAppExeName "SistemaVentas.exe"

[Setup]
AppId={{B9E568FA-813D-4C75-AAB6-78A886523111}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={localappdata}\Programs\SistemaVentas
DefaultGroupName={#MyAppName}
OutputDir=Instalador\Salida
OutputBaseFilename=SistemaDeVentas-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x86 x64

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[Files]
Source: "CapaPresentacion\bin\Release\*"; DestDir: "{app}"; Excludes: "conexion.txt,conexion.ejemplo.txt,query.sql,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
; Se instala la plantilla como conexion.txt solo la primera vez. Las actualizaciones preservan la conexión existente.
Source: "CapaPresentacion\conexion.ejemplo.txt"; DestDir: "{app}"; DestName: "conexion.txt"; Flags: onlyifdoesntexist
Source: "PASOS_USUARIO_SQL_SERVER.txt"; DestDir: "{app}"; DestName: "LEEME_CONFIGURACION_SQL.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; WorkingDir: "{app}"; Flags: postinstall nowait skipifsilent
