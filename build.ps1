$ErrorActionPreference = 'Stop'
$saida = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force -Path $saida | Out-Null
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $csc /nologo /target:library /platform:x86 /optimize+ "/out:$saida\Leitor.dll" (Join-Path $PSScriptRoot 'Leitor.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar Leitor.dll' }
& $csc /nologo /target:exe /platform:x86 /optimize+ "/reference:$saida\Leitor.dll" "/out:$saida\Capturar.exe" (Join-Path $PSScriptRoot 'Capturar.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar Capturar.exe' }
foreach ($nome in @('fpengine.dll', 'fpcore32.dll', 'fpspeed32.dll', 'nbis32.dll')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "dlls\$nome") -Destination $saida
}
Write-Output "SDK em $saida"
