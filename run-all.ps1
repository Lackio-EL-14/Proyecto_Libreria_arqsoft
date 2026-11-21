#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Levanta a la vez el Front, el gateway y los 4 microservicios (US-110).

.DESCRIPTION
    1. Compila la solución una sola vez.
    2. Arranca cada proyecto con `dotnet run --no-build`:
       en Windows, cada uno en su propia ventana; en Linux y macOS, en segundo plano con su log.
    3. Comprueba GET /salud de cada servicio y GET / del Front, y muestra una tabla.
    4. Espera hasta Ctrl+C y, al salir, detiene todos los procesos.

    Las bases de datos se levantan aparte con `docker compose up -d` (ver README).
    La configuración sale del mismo `.env` que usa Docker Compose: las cadenas
    ConnectionStrings__* (si faltan, se arman con las contraseñas de las bases) y el
    secreto de Keycloak que necesita cada proceso. Nada se imprime en pantalla.

.PARAMETER SinCompilar
    No ejecuta `dotnet build` antes de arrancar (úsalo si acabas de compilar).

.EXAMPLE
    ./run-all.ps1
#>
param(
    [switch] $SinCompilar
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Read-DotEnv([string] $ruta) {
    $valores = @{}
    if (-not (Test-Path $ruta)) {
        throw "No existe $ruta. Copie .env.example como .env y complete los valores (ver README)."
    }
    foreach ($linea in Get-Content $ruta) {
        $texto = $linea.Trim()
        if ($texto -eq '' -or $texto.StartsWith('#') -or -not $texto.Contains('=')) { continue }
        $indice = $texto.IndexOf('=')
        $valores[$texto.Substring(0, $indice).Trim()] = $texto.Substring($indice + 1).Trim().Trim('"')
    }
    return $valores
}

function Get-Valor([hashtable] $entorno, [string] $clave, [string] $porDefecto) {
    if ($entorno.ContainsKey($clave) -and $entorno[$clave] -and -not $entorno[$clave].Contains('<')) {
        return $entorno[$clave]
    }
    return $porDefecto
}

$entorno = Read-DotEnv (Join-Path $PSScriptRoot '.env')

$cadenas = @{
    'ConnectionStrings__ProveedoresDb' = Get-Valor $entorno 'ConnectionStrings__ProveedoresDb' "Server=localhost,1433;Database=LibreriaProveedores;User Id=sa;Password=$($entorno['PROVEEDORES_DB_SA_PASSWORD']);TrustServerCertificate=True;"
    'ConnectionStrings__UsuariosDb'    = Get-Valor $entorno 'ConnectionStrings__UsuariosDb' "Server=localhost,1434;Database=LibreriaUsuarios;User Id=sa;Password=$($entorno['USUARIOS_DB_SA_PASSWORD']);TrustServerCertificate=True;"
    'ConnectionStrings__ClientesDb'    = Get-Valor $entorno 'ConnectionStrings__ClientesDb' "Server=localhost;Port=3308;Database=libreria_clientes;User ID=$($entorno['CLIENTES_DB_USUARIO']);Password=$($entorno['CLIENTES_DB_PASSWORD']);"
    'ConnectionStrings__ProductosDb'   = Get-Valor $entorno 'ConnectionStrings__ProductosDb' 'mongodb://localhost:27017/libreria_productos'
}

$proyectos = @(
    @{ Nombre = 'Proveedores'; Proyecto = 'src/Libreria.Servicio.Proveedores'; Url = 'http://localhost:5101/salud'
       Variables = @{ 'ConnectionStrings__ProveedoresDb' = $cadenas['ConnectionStrings__ProveedoresDb'] } }
    @{ Nombre = 'Usuarios';  Proyecto = 'src/Libreria.Servicio.Usuarios';  Url = 'http://localhost:5102/salud'
       Variables = @{
           'ConnectionStrings__UsuariosDb'      = $cadenas['ConnectionStrings__UsuariosDb']
           'Keycloak__ClientSecret'             = $entorno['KEYCLOAK_USUARIOS_ADMIN_CLIENT_SECRET']
           'Keycloak__VerificacionClientSecret' = $entorno['KEYCLOAK_USUARIOS_VERIFICACION_CLIENT_SECRET']
       } }
    @{ Nombre = 'Clientes';  Proyecto = 'src/Libreria.Servicio.Clientes';  Url = 'http://localhost:5103/salud'
       Variables = @{ 'ConnectionStrings__ClientesDb' = $cadenas['ConnectionStrings__ClientesDb'] } }
    @{ Nombre = 'Productos'; Proyecto = 'src/Libreria.Servicio.Productos'; Url = 'http://localhost:5104/salud'
       Variables = @{ 'ConnectionStrings__ProductosDb' = $cadenas['ConnectionStrings__ProductosDb'] } }
    @{ Nombre = 'Gateway';   Proyecto = 'src/Libreria.Gateway';            Url = 'http://localhost:5080/salud'
       Variables = @{} }
    @{ Nombre = 'Front';     Proyecto = 'src/Libreria.Client';             Url = 'http://localhost:5000/'
       Variables = @{ 'Keycloak__ClientSecret' = $entorno['KEYCLOAK_FRONT_CLIENT_SECRET'] } }
)

# Variables opcionales que el MS Usuarios lee directamente (correo).
foreach ($clave in 'BASE_URL', 'SMTP_HOST', 'SMTP_PORT', 'SMTP_USER', 'SMTP_PASS', 'SMTP_FROM') {
    $valor = Get-Valor $entorno $clave $null
    if ($valor) { $proyectos[1].Variables[$clave] = $valor }
}

# Con un SMTP completo en .env, el alta de usuarios envía el correo de verdad (US-130).
# Si falta o tiene los valores de ejemplo, el correo se guarda en la carpeta local de appsettings.Development.json.
$smtpCompleto = @('SMTP_HOST', 'SMTP_PORT', 'SMTP_USER', 'SMTP_PASS', 'SMTP_FROM' |
    ForEach-Object { Get-Valor $entorno $_ $null } |
    Where-Object { $_ -and $_ -notmatch 'ejemplo|tu_usuario_smtp|tu_password_smtp' })
if ($smtpCompleto.Count -eq 5) {
    $proyectos[1].Variables['EMAIL_DELIVERY_MODE'] = 'Smtp'
    Write-Host 'Correo de Usuarios: modo SMTP (correo real).' -ForegroundColor Cyan
} else {
    Write-Host 'Correo de Usuarios: SMTP incompleto en .env; los correos se guardan en src/Libreria.Servicio.Usuarios/bin/Development/mail.' -ForegroundColor Yellow
}

if (-not $SinCompilar) {
    Write-Host 'Compilando LibreriaSystem.slnx...' -ForegroundColor Cyan
    dotnet build LibreriaSystem.slnx --nologo -v quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'La compilación falló; no se arranca nada.' -ForegroundColor Red
        exit 1
    }
}

# Windows PowerShell 5.1 no define $IsWindows ni -SkipHttpErrorCheck: el script funciona en 5.1 y en PowerShell 7.
$esWindows = $IsWindows -or $PSVersionTable.PSEdition -eq 'Desktop'
$esPowerShell7 = $PSVersionTable.PSVersion.Major -ge 7

$carpetaLogs = Join-Path ([System.IO.Path]::GetTempPath()) 'libreria-run-all'
if (-not $esWindows) {
    New-Item -ItemType Directory -Force -Path $carpetaLogs | Out-Null
}

$procesos = @()

try {
    foreach ($p in $proyectos) {
        $argumentos = @('run', '--no-build', '--project', $p.Proyecto)

        # Cada proceso hereda solo sus variables: se fijan antes de lanzarlo y se quitan después.
        foreach ($variable in $p.Variables.GetEnumerator()) {
            [Environment]::SetEnvironmentVariable($variable.Key, $variable.Value)
        }

        if ($esWindows) {
            $proceso = Start-Process dotnet -ArgumentList $argumentos -PassThru
        }
        else {
            $log = Join-Path $carpetaLogs "$($p.Nombre).log"
            $proceso = Start-Process dotnet -ArgumentList $argumentos -PassThru `
                -RedirectStandardOutput $log -RedirectStandardError "$log.err"
        }

        foreach ($variable in $p.Variables.GetEnumerator()) {
            [Environment]::SetEnvironmentVariable($variable.Key, $null)
        }

        $procesos += $proceso
        Write-Host ("Iniciado {0,-10} (PID {1})" -f $p.Nombre, $proceso.Id)
    }

    Write-Host ''
    Write-Host 'Comprobando que cada proyecto responda...' -ForegroundColor Cyan

    $resumen = foreach ($p in $proyectos) {
        $estado = 'FALLA'
        $limite = (Get-Date).AddSeconds(60)

        while ((Get-Date) -lt $limite) {
            $codigo = $null
            try {
                if ($esPowerShell7) {
                    $codigo = [int](Invoke-WebRequest -Uri $p.Url -TimeoutSec 2 -SkipHttpErrorCheck).StatusCode
                }
                else {
                    $codigo = [int](Invoke-WebRequest -Uri $p.Url -TimeoutSec 2 -UseBasicParsing).StatusCode
                }
            }
            catch {
                # En 5.1 un 4xx llega como excepción con respuesta; sin respuesta, todavía no escucha y se reintenta.
                if ($_.Exception.Response) { $codigo = [int]$_.Exception.Response.StatusCode }
            }
            if ($codigo -and $codigo -lt 500) {
                $estado = "OK ($codigo)"
                break
            }
            Start-Sleep -Milliseconds 500
        }

        [pscustomobject]@{ Proyecto = $p.Nombre; Url = $p.Url; Estado = $estado }
    }

    $resumen | Format-Table -AutoSize | Out-Host

    if ($resumen.Estado -contains 'FALLA') {
        Write-Host 'Algún proyecto no respondió.' -ForegroundColor Yellow
        if (-not $esWindows) { Write-Host "Revisa los logs en $carpetaLogs" -ForegroundColor Yellow }
    }
    else {
        Write-Host 'Los 6 proyectos están arriba. Front: http://localhost:5000' -ForegroundColor Green
    }

    Write-Host 'Presiona Ctrl+C para detener todo.' -ForegroundColor Cyan
    while ($true) { Start-Sleep -Seconds 1 }
}
finally {
    Write-Host ''
    Write-Host 'Deteniendo los proyectos...' -ForegroundColor Cyan
    foreach ($proceso in $procesos) {
        if (-not $proceso.HasExited) {
            # `dotnet run` lanza la aplicación como proceso hijo: se detiene el árbol completo.
            if ($esPowerShell7) { $proceso.Kill($true) }
            else { taskkill /PID $proceso.Id /T /F | Out-Null }
        }
    }
}
