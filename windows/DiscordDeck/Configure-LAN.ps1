$ErrorActionPreference='Stop'
$exe=Join-Path $PSScriptRoot 'DiscordDeck.exe'
if(!(Test-Path -LiteralPath $exe)){throw 'DiscordDeck.exe fehlt'}
foreach($protocol in @('TCP','UDP')){
 $port=if($protocol -eq 'TCP'){47836}else{47837}
 $name="DiscordDeck LAN $protocol"
 Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue | Remove-NetFirewallRule
 New-NetFirewallRule -DisplayName $name -Direction Inbound -Action Allow -Profile Private -Protocol $protocol -LocalPort $port -RemoteAddress LocalSubnet -Program $exe | Out-Null
}
