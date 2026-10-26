[CmdletBinding()]
param([string]$InstallRoot='D:\Apps\DragonCodexBoot')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$qa=Join-Path $root '.local/desktop-qa'
New-Item -ItemType Directory -Force $qa | Out-Null
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$testExe=Join-Path $qa 'DesktopTests.exe'
$refs=@('System.dll','System.Core.dll','System.Xaml.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll')
foreach($name in @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll')) { $refs+=Join-Path $framework ('WPF/'+$name) }
$argsList=@('/nologo','/target:exe','/platform:x64','/main:DesktopTests',('/win32manifest:'+(Join-Path $root 'src/app.manifest')),('/out:'+$testExe))
foreach($reference in $refs) {$argsList+='/reference:'+$reference}
$argsList+=Join-Path $root 'src/DragonCodexBoot.cs'
$argsList+=Join-Path $root 'tests/DesktopTests.cs'
& (Join-Path $framework 'csc.exe') @argsList
if($LASTEXITCODE -ne 0) {throw 'Compile failed'}
$originalConfig=Join-Path $InstallRoot 'launcher.json'
$config=Get-Content -LiteralPath $originalConfig -Raw | ConvertFrom-Json
$video=[IO.Path]::GetFullPath((Join-Path $InstallRoot $config.Video))
foreach($case in @(@('DISPLAY1','normal'),@('DISPLAY2','normal'),@('DISPLAY2','short'),@('DISPLAY2','missing'))) {
 $run=Join-Path $qa ($case[0]+'-'+$case[1])
 New-Item -ItemType Directory -Force $run | Out-Null
 Copy-Item -LiteralPath $originalConfig -Destination (Join-Path $run 'launcher.json')
 & $testExe $run ('\\.\'+$case[0]) $case[1] $video
 if($LASTEXITCODE -ne 0) {throw ('Desktop test failed: '+($case -join ' '))}
}
