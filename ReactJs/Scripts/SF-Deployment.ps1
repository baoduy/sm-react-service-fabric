#The Deploy-FabricApplication.ps1 is in the Service Fabric Projects. Need to embeded along with the package.
$DeployFabricApplicationFilePath = ".\Deploy-FabricApplication.ps1"

$ServiceFabricConnectionEndpoint = 'localhost:19000'
$ApplicationManifestFilePath = ".\ApplicationManifest.xml"
$PublishProfileFilePath = ".\PublishProfiles\Publish.xml"
$ApplicationPackagePath = ".\pkg\Release"
$OverrideUpgradeBehavior = 'ForceUpgrade'
$OverwriteBehavior = 'Always'
$InstallationDirectory = '.\'
$imageStoreConnectionString = "fabric:ImageStore"
$ErrorActionPreference = "Stop"

"Connecting using `"$ServiceFabricConnectionEndpoint`""
$ConnectionResult = Connect-ServiceFabricCluster -ConnectionEndpoint $ServiceFabricConnectionEndpoint -WindowsCredential
Write-Verbose "Connection result: $( $ConnectionResult | Out-String )"
If ( $ConnectionResult.Count -ne 2 -and -not $ConnectionResult[0] ) {
    Throw "Connection to Service Fabric failed: $( $ConnectionResult | Out-String )"
}

$ApplicationManifestXml = [xml]( Get-Content ( Join-Path $InstallationDirectory $ApplicationManifestFilePath ) )
$ApplicationTypeName = $ApplicationManifestXml.ApplicationManifest.ApplicationTypeName
"ApplicationTypeName: $ApplicationTypeName"

$DeployedApplication = Get-ServiceFabricApplication -ApplicationTypeName "$ApplicationTypeName"
Write-Verbose "Existing application: $( $DeployedApplication | Out-String )"
$IsUpdateRequired = $DeployedApplication -ne $null
"Application exists on the cluster already: $IsUpdateRequired"

if ( -not $IsUpdateRequired ) {
    "Application doesn't exist, changing OverrideUpgradeBehavior from $OverrideUpgradeBehavior to `"None`""
    $OverrideUpgradeBehavior = "None"
}
else
{
    $services = Get-ServiceFabricService  -ApplicationName $DeployedApplication.ApplicationName 
    foreach($service in $services)
    {
    	$serviceFound =$ApplicationManifestXml.ApplicationManifest.DefaultServices.GetElementsByTagName("Service") |  
    			Where {$_.getAttributeNode('Name').Value -eq $service.ServiceName.AbsolutePath.Split("/")[-1]}
    	if([string]::IsNullOrEmpty($serviceFound))
    	{
    		Write-Verbose "Removing service: $( $service.ServiceName | Out-String )"
    		Remove-ServiceFabricService -ServiceName $service.ServiceName -Force
    	}
    }
}


"Opening PublishProfile `"$PublishProfileFilePath`""
$PublishProfileXml = [xml]( Get-Content ( Join-Path $InstallationDirectory $PublishProfileFilePath ) )
"Setting the UpgradeDeployment setting to $IsUpdateRequired"
$PublishProfileXml.PublishProfile.UpgradeDeployment.Enabled = $IsUpdateRequired.ToString()
$PublishProfileXml.Save( ( Join-Path $InstallationDirectory $PublishProfileFilePath ) )

"Running script `"$DeployFabricApplicationFilePath`""
& ( Join-Path $InstallationDirectory $DeployFabricApplicationFilePath ) `
    -PublishProfileFile ( Join-Path $InstallationDirectory $PublishProfileFilePath ) `
    -ApplicationPackagePath ( Join-Path $InstallationDirectory $ApplicationPackagePath ) `
    -OverrideUpgradeBehavior $OverrideUpgradeBehavior `
    -OverwriteBehavior $OverwriteBehavior `
    -UnregisterUnusedApplicationVersionsAfterUpgrade $true `
    -UseExistingClusterConnection
