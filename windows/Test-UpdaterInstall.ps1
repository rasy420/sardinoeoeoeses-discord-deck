param([Parameter(Mandatory=$true)][string]$Installer)
$ErrorActionPreference='Stop'
$Installer=(Resolve-Path -LiteralPath $Installer).Path
$testRoot=Join-Path $PSScriptRoot ('installer-regression-'+[Guid]::NewGuid().ToString('N'))
$app=Join-Path $testRoot 'App'
New-Item -ItemType Directory -Path $app -Force | Out-Null
$config=Join-Path $testRoot 'settings.json'
[IO.File]::WriteAllText($config,'{"TourDone":true,"Slots":[{"Name":"Preserved name","CustomName":true,"Volume":2.37,"TrimStart":0.123456,"TrimEnd":1.5}]}')
$before=(Get-FileHash -LiteralPath $config).Hash
$previousHome=$env:DISCORDDECK_HOME
try {
 $env:DISCORDDECK_HOME=$testRoot
 foreach($phase in @('fresh','update')) {
  # Match Process.Start from the running soundboard: inherited working directory is App.
  $p=Start-Process -FilePath $Installer -WorkingDirectory $app -ArgumentList '--install','--no-launch' -WindowStyle Hidden -Wait -PassThru
  if($p.ExitCode -ne 0){throw "$phase install failed (exit $($p.ExitCode))"}
  if((Get-FileHash -LiteralPath $config).Hash -ne $before){throw 'Settings changed during installation'}
  if(-not (Test-Path -LiteralPath (Join-Path $app 'DiscordDeck.exe'))){throw 'Installed executable missing'}
  Write-Output "PASS: $phase installation from App working directory; settings unchanged"
 }
} finally { $env:DISCORDDECK_HOME=$previousHome }
