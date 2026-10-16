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
; Plantilla de referencia. El asistente crea conexion.txt con los datos ingresados.
Source: "CapaPresentacion\conexion.ejemplo.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "PASOS_USUARIO_SQL_SERVER.txt"; DestDir: "{app}"; DestName: "LEEME_CONFIGURACION_SQL.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; WorkingDir: "{app}"; Flags: postinstall nowait skipifsilent; Check: ShouldLaunchApplication

[Code]
var
  ConnectionPage: TInputQueryWizardPage;
  ConnectionSaved: Boolean;

procedure InitializeWizard();
begin
  ConnectionPage := CreateInputQueryPage(wpSelectDir,
    'Conexión con la base de datos',
    'Indica dónde está SQL Server y las credenciales de la aplicación.',
    'La contraseña se guardará en conexion.txt junto al programa. No compartas ni subas ese archivo.');
  ConnectionPage.Add('IP o nombre del servidor (sin puerto):', False);
  ConnectionPage.Add('Puerto TCP:', False);
  ConnectionPage.Add('Usuario de SQL Server:', False);
  ConnectionPage.Add('Contraseña de SQL Server:', True);
  ConnectionPage.Values[1] := '1433';
  ConnectionPage.Values[2] := 'ventas_app2';
  ConnectionSaved := False;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Puerto: Integer;
begin
  Result := True;
  if CurPageID = ConnectionPage.ID then
  begin
    if Trim(ConnectionPage.Values[0]) = '' then
    begin
      MsgBox('Escribe la IP o el nombre del servidor SQL Server.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    Puerto := StrToIntDef(Trim(ConnectionPage.Values[1]), 0);
    if (Puerto < 1) or (Puerto > 65535) then
    begin
      MsgBox('El puerto debe ser un número entre 1 y 65535.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if Trim(ConnectionPage.Values[2]) = '' then
    begin
      MsgBox('Escribe el usuario de SQL Server.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if ConnectionPage.Values[3] = '' then
    begin
      MsgBox('Escribe la contraseña de SQL Server.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;
end;

function QuoteConnectionValue(Value: String): String;
begin
  StringChangeEx(Value, '"', '""', True);
  Result := '"' + Value + '"';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ConnectionString: String;
  ConnectionLines: TArrayOfString;
begin
  if CurStep = ssPostInstall then
  begin
    ConnectionString :=
      'Data Source=' + QuoteConnectionValue(Trim(ConnectionPage.Values[0]) + ',' + Trim(ConnectionPage.Values[1])) +
      ';Initial Catalog=DBSISTEMA_VENTA' +
      ';User ID=' + QuoteConnectionValue(Trim(ConnectionPage.Values[2])) +
      ';Password=' + QuoteConnectionValue(ConnectionPage.Values[3]) +
      ';Encrypt=False;TrustServerCertificate=True;';
    SetArrayLength(ConnectionLines, 1);
    ConnectionLines[0] := ConnectionString;
    ConnectionSaved := SaveStringsToUTF8FileWithoutBOM(
      ExpandConstant('{app}\conexion.txt'), ConnectionLines, False);
    if not ConnectionSaved then
      MsgBox('No se pudo guardar conexion.txt. Ejecuta el instalador en una carpeta donde tu usuario pueda escribir.', mbError, MB_OK);
  end;
end;

function ShouldLaunchApplication(): Boolean;
begin
  Result := ConnectionSaved;
end;
