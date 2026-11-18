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

$proyectos = @(
    @{ Nombre = 'Proveedores'; Proyecto = 'src/Libreria.Servicio.Proveedores'; Url = 'http://localhost:5101/salud' }
    @{ Nombre = 'Usuarios';  Proyecto = 'src/Libreria.Servicio.Usuarios';  Url = 'http://localhost:5102/salud' }
    @{ Nombre = 'Clientes';  Proyecto = 'src/Libreria.Servicio.Clientes';  Url = 'http://localhost:5103/salud' }
    @{ Nombre = 'Productos'; Proyecto = 'src/Libreria.Servicio.Productos'; Url = 'http://localhost:5104/salud' }
    @{ Nombre = 'Gateway';   Proyecto = 'src/Libreria.Gateway';            Url = 'http://localhost:5080/salud' }
    @{ Nombre = 'Front';     Proyecto = 'src/Libreria.Client';             Url = 'http://localhost:5000/' }
)

if (-not $SinCompilar) {
    Write-Host 'Compilando LibreriaSystem.slnx...' -ForegroundColor Cyan
    dotnet build LibreriaSystem.slnx --nologo -v quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'La compilación falló; no se arranca nada.' -ForegroundColor Red
        exit 1
    }
}

$carpetaLogs = Join-Path ([System.IO.Path]::GetTempPath()) 'libreria-run-all'
if (-not $IsWindows) {
    New-Item -ItemType Directory -Force -Path $carpetaLogs | Out-Null
}

$procesos = @()

try {
    foreach ($p in $proyectos) {
        $argumentos = @('run', '--no-build', '--project', $p.Proyecto)

        if ($IsWindows) {
            $proceso = Start-Process dotnet -ArgumentList $argumentos -PassThru
        }
        else {
            $log = Join-Path $carpetaLogs "$($p.Nombre).log"
            $proceso = Start-Process dotnet -ArgumentList $argumentos -PassThru `
                -RedirectStandardOutput $log -RedirectStandardError "$log.err"
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
            try {
                $respuesta = Invoke-WebRequest -Uri $p.Url -TimeoutSec 2 -SkipHttpErrorCheck
                if ($respuesta.StatusCode -lt 500) {
                    $estado = "OK ($($respuesta.StatusCode))"
                    break
                }
            }
            catch {
                # Todavía no escucha: se reintenta.
            }
            Start-Sleep -Milliseconds 500
        }

        [pscustomobject]@{ Proyecto = $p.Nombre; Url = $p.Url; Estado = $estado }
    }

    $resumen | Format-Table -AutoSize | Out-Host

    if ($resumen.Estado -contains 'FALLA') {
        Write-Host 'Algún proyecto no respondió.' -ForegroundColor Yellow
        if (-not $IsWindows) { Write-Host "Revisa los logs en $carpetaLogs" -ForegroundColor Yellow }
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
            $proceso.Kill($true)
        }
    }
}
