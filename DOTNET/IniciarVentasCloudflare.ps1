$ErrorActionPreference = 'Stop'

# Este script inicia la aplicación publicada, crea un Quick Tunnel y actualiza
# únicamente la clave "url" del namespace KV de ventas.
$accountId = 'c6c07a9df41384152c3693988b0e10a7'
$namespaceId = '85d980afddfa47ebab99e4b440c530a2'
$port = 5170
$projectFile = Join-Path $PSScriptRoot 'ImagenPDF.csproj'
$tokenPath = Join-Path $PSScriptRoot 'cloudflare-token.txt'
$tokenPathWithoutExtension = Join-Path $PSScriptRoot 'cloudflare-token'
$publishedPath = Join-Path $PSScriptRoot 'publicado'
$applicationDll = Join-Path $publishedPath 'ImagenPDF.dll'
$logsPath = Join-Path $PSScriptRoot 'logs'
$launcherLog = Join-Path $logsPath 'lanzador.log'
$stopFile = Join-Path $logsPath 'detener-servidor.flag'
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$serverOut = Join-Path $logsPath "ventas-servidor-$stamp-out.log"
$serverErr = Join-Path $logsPath "ventas-servidor-$stamp-error.log"
$tunnelOut = Join-Path $logsPath "ventas-tunel-$stamp-out.log"
$tunnelErr = Join-Path $logsPath "ventas-tunel-$stamp-error.log"

$serverProcess = $null
$tunnelProcess = $null
$startedServer = $false
$hasPushedLocation = $false
$stopRequested = $false
$mutex = New-Object -TypeName System.Threading.Mutex -ArgumentList @($false, 'Local\SistemaVentasCloudflare')
$ownsMutex = $false

try {
    $ownsMutex = $mutex.WaitOne(0)
}
catch [System.Threading.AbandonedMutexException] {
    $ownsMutex = $true
}

if (-not $ownsMutex) {
    Write-Host 'El servidor de ventas ya está iniciado por otro proceso.'
    $mutex.Dispose()
    exit 0
}

function Test-PortOpen {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect('127.0.0.1', $port, $null, $null)
        if (-not $async.AsyncWaitHandle.WaitOne(500)) { return $false }
        $client.EndConnect($async)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Close()
    }
}

function Write-RunLog {
    param([string]$Message)
    $line = '[{0}] {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Message
    Add-Content -LiteralPath $launcherLog -Value $line -Encoding UTF8
    Write-Host $Message
}

function Get-TunnelUrlFromLogs {
    $text = ''
    foreach ($path in @($tunnelOut, $tunnelErr)) {
        if (Test-Path -LiteralPath $path) {
            try { $text += "`n" + (Get-Content -LiteralPath $path -Raw -ErrorAction Stop) } catch {}
        }
    }
    $match = [regex]::Match($text, 'https://[a-z0-9-]+\.trycloudflare\.com', 'IgnoreCase')
    if ($match.Success) { return $match.Value.TrimEnd('.') }
    return $null
}

try {
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
    Remove-Item -LiteralPath $stopFile -Force -ErrorAction SilentlyContinue

    if (-not (Test-Path -LiteralPath $projectFile)) {
        throw "No encuentro el proyecto $projectFile."
    }

    # Mantiene disponibles el logo y los recursos PWA servidos por la versión publicada.
    $sourceWebRoot = Join-Path $PSScriptRoot 'wwwroot'
    $publishedWebRoot = Join-Path $publishedPath 'wwwroot'
    $webAssets = @(
        'favicon.png', 'favicon.ico', 'icon-192.png', 'icon-512.png',
        'manifest.webmanifest', 'sw.js', 'offline.html', 'js\pwa.js'
    )
    foreach ($asset in $webAssets) {
        $sourceAsset = Join-Path $sourceWebRoot $asset
        if (Test-Path -LiteralPath $sourceAsset) {
            $targetAsset = Join-Path $publishedWebRoot $asset
            $targetDirectory = Split-Path -Parent $targetAsset
            New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
            Copy-Item -LiteralPath $sourceAsset -Destination $targetAsset -Force
        }
    }
    if (-not (Test-Path -LiteralPath $tokenPath) -and (Test-Path -LiteralPath $tokenPathWithoutExtension)) {
        $tokenPath = $tokenPathWithoutExtension
    }
    if (-not (Test-Path -LiteralPath $tokenPath)) {
        throw "No encuentro cloudflare-token.txt junto a este script. Coloca allí una copia del archivo con tu API Token."
    }

    $token = (Get-Content -LiteralPath $tokenPath -Raw).Trim()
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw 'cloudflare-token.txt está vacío.'
    }

    $dotnet = Get-Command 'dotnet.exe' -ErrorAction SilentlyContinue
    if (-not $dotnet) { $dotnet = Get-Command 'dotnet' -ErrorAction SilentlyContinue }
    if (-not $dotnet) { throw 'No encuentro dotnet. Instala .NET o agrega dotnet al PATH de Windows.' }
    $dotnetExecutable = if ($dotnet.Source) { $dotnet.Source } else { $dotnet.Path }
    if (-not $dotnetExecutable) { throw 'No pude determinar la ruta del ejecutable dotnet.' }

    $cloudflared = Get-Command 'cloudflared.exe' -ErrorAction SilentlyContinue
    if (-not $cloudflared) { $cloudflared = Get-Command 'cloudflared' -ErrorAction SilentlyContinue }
    if (-not $cloudflared) {
        $cloudflaredPaths = @(
            (Join-Path $PSScriptRoot 'cloudflared.exe'),
            (Join-Path $env:ProgramFiles 'Cloudflare\cloudflared.exe'),
            (Join-Path ${env:ProgramFiles(x86)} 'Cloudflare\cloudflared.exe'),
            'C:\ServidorVideos\cloudflared.exe'
        )
        $cloudflaredPath = $cloudflaredPaths | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
        if ($cloudflaredPath) {
            $cloudflared = Get-Item -LiteralPath $cloudflaredPath
        }
    }
    if (-not $cloudflared) { throw 'No encuentro cloudflared. Agrega cloudflared.exe al PATH o déjalo junto a este script.' }
    $cloudflaredExecutable = if ($cloudflared.Source) { $cloudflared.Source } else { $cloudflared.FullName }

    if (Test-PortOpen) {
        throw "El puerto $port ya está ocupado. Cierra primero la versión anterior o el servidor iniciado manualmente; no iniciaré un proceso desconocido para evitar publicar una versión vieja."
    }

    # Ejecuta el mismo proyecto y el SDK indicado por DOTNET\global.json,
    # aunque el lanzador se abra desde otra carpeta o desde Inicio de Windows.
    Push-Location -LiteralPath $PSScriptRoot
    $hasPushedLocation = $true
    Write-RunLog "Proyecto: $projectFile"
    Write-RunLog "Ejecutable dotnet: $dotnetExecutable"
    $sdkVersion = (& $dotnetExecutable --version 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw "No se pudo consultar la versión del SDK con $dotnetExecutable." }
    Write-RunLog "SDK seleccionado por global.json: $sdkVersion"
    Write-RunLog 'Compilando y publicando la versión actual antes de iniciar...'
    $publishOutput = & $dotnetExecutable publish $projectFile --configuration Release --output $publishedPath --nologo 2>&1
    $publishExitCode = $LASTEXITCODE
    foreach ($line in $publishOutput) {
        Add-Content -LiteralPath $launcherLog -Value ('[publish] {0}' -f $line) -Encoding UTF8
    }
    if ($publishExitCode -ne 0) {
        throw "dotnet publish terminó con código $publishExitCode. Revisa los detalles en $launcherLog."
    }
    if (-not (Test-Path -LiteralPath $applicationDll)) {
        throw "La publicación terminó pero no apareció $applicationDll. Revisa $launcherLog."
    }

    Write-RunLog 'Iniciando la versión recién publicada...'
    $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $previousProductImagesPath = $env:ProductImages__Path
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:ProductImages__Path = Join-Path $PSScriptRoot 'imagenes'
    try {
        $serverProcess = Start-Process -FilePath $dotnetExecutable `
            -ArgumentList @('ImagenPDF.dll', '--urls', "http://0.0.0.0:$port") `
            -WorkingDirectory $publishedPath -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput $serverOut -RedirectStandardError $serverErr
    }
    finally {
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
        $env:ProductImages__Path = $previousProductImagesPath
    }
    $startedServer = $true

    $deadline = (Get-Date).AddSeconds(60)
    while (-not (Test-PortOpen) -and (Get-Date) -lt $deadline) {
        if ($serverProcess.HasExited) {
            throw "La aplicación se cerró al iniciar. Revisa los registros en $logsPath."
        }
        Start-Sleep -Seconds 1
    }
    if (-not (Test-PortOpen)) { throw "La aplicación no abrió el puerto $port en 60 segundos. Revisa los registros en $logsPath." }

    Write-RunLog 'Iniciando el túnel temporal de Cloudflare...'
    $tunnelProcess = Start-Process -FilePath $cloudflaredExecutable `
        -ArgumentList @('tunnel', '--url', "http://localhost:$port") `
        -WindowStyle Hidden -PassThru -RedirectStandardOutput $tunnelOut -RedirectStandardError $tunnelErr

    $tunnelUrl = $null
    $deadline = (Get-Date).AddSeconds(90)
    while (-not $tunnelUrl -and (Get-Date) -lt $deadline) {
        if ($tunnelProcess.HasExited) { throw "cloudflared se cerró. Revisa los registros en $logsPath." }
        $tunnelUrl = Get-TunnelUrlFromLogs
        if (-not $tunnelUrl) { Start-Sleep -Seconds 1 }
    }
    if (-not $tunnelUrl) { throw "No apareció una URL trycloudflare.com en 90 segundos. Revisa los registros en $logsPath." }

    Write-RunLog "Túnel creado: $tunnelUrl"
    Write-RunLog 'Actualizando la clave url del KV de ventas...'
    $apiUrl = "https://api.cloudflare.com/client/v4/accounts/$accountId/storage/kv/namespaces/$namespaceId/values/url"
    $headers = @{ Authorization = "Bearer $token" }
    $body = [System.Text.Encoding]::UTF8.GetBytes($tunnelUrl)
    $apiResult = Invoke-RestMethod -Uri $apiUrl -Method Put -Headers $headers `
        -ContentType 'application/octet-stream' -Body $body -TimeoutSec 30

    if (-not $apiResult.success) { throw 'Cloudflare no confirmó la actualización de KV.' }

    Write-RunLog 'Listo. La URL del túnel ya se guardó en KV.'
    Write-RunLog 'El servicio está en segundo plano. Para detenerlo, ejecuta DetenerVentas.bat.'

    while (-not $tunnelProcess.HasExited) {
        if (Test-Path -LiteralPath $stopFile) {
            Write-RunLog 'Se recibió la solicitud para detener el servidor y el túnel.'
            $stopRequested = $true
            break
        }
        if ($startedServer -and $serverProcess.HasExited) {
            throw "La aplicación se detuvo. Revisa los registros en $logsPath."
        }
        Start-Sleep -Seconds 2
    }
    if (-not $stopRequested) { throw 'El proceso cloudflared terminó.' }
}
catch {
    $errorMessage = "No se pudo completar el inicio: $($_.Exception.Message)"
    Write-Host $errorMessage -ForegroundColor Red
    if (Test-Path -LiteralPath $logsPath) {
        Add-Content -LiteralPath $launcherLog -Value ('[{0}] {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $errorMessage) -Encoding UTF8
        Add-Content -LiteralPath $launcherLog -Value $_.ScriptStackTrace -Encoding UTF8
        Write-Host "Registros: $logsPath"
    }
    throw
}
finally {
    if ($tunnelProcess -and -not $tunnelProcess.HasExited) {
        Stop-Process -Id $tunnelProcess.Id -Force -ErrorAction SilentlyContinue
    }
    if ($startedServer -and $serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path -LiteralPath $stopFile) {
        Remove-Item -LiteralPath $stopFile -Force -ErrorAction SilentlyContinue
    }
    if ($hasPushedLocation) { Pop-Location }
    if ($ownsMutex) { $mutex.ReleaseMutex() }
    $mutex.Dispose()
}
