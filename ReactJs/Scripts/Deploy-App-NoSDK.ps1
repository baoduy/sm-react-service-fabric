# Variables
$endpoint = 'localhost:19000'
$packagepath="C:\Users\SFAdmin\Downloads\ReactJs"
$appName = 'ReactJs'
$appVersion = '1.0.0'
$thumbprint = ''

if([string]::IsNullOrEmpty( $thumbprint)){

	Write-Verbose "Connect to $endpoint using Window Account"
	$ConnectionResult = Connect-ServiceFabricCluster -ConnectionEndpoint $endpoint -WindowsCredential
}else{

	Write-Verbose "Connect to $endpoint using Certificate"

	$ConnectionResult = Connect-ServiceFabricCluster -ConnectionEndpoint $endpoint `
          -KeepAliveIntervalInSec 10 `
          -X509Credential -ServerCertThumbprint $Thumbprint `
          -FindType FindByThumbprint -FindValue $Thumbprint `
          -StoreLocation CurrentUser -StoreName My
}

Write-Verbose "Connection result: $( $ConnectionResult | Out-String )"
If ( $ConnectionResult.Count -ne 2 -and -not $ConnectionResult[0] ) {
    Throw "Connection to Service Fabric failed: $( $ConnectionResult | Out-String )"
}

# Copy the application package to the cluster image store.
Copy-ServiceFabricApplicationPackage $packagepath -ImageStoreConnectionString fabric:ImageStore -ApplicationPackagePathInImageStore $appName

# Register the application type.
Register-ServiceFabricApplicationType -ApplicationPathInImageStore $appName

# Remove the application package to free system resources.
Remove-ServiceFabricApplicationPackage -ImageStoreConnectionString fabric:ImageStore -ApplicationPackagePathInImageStore $appName

# Create the application instance.
New-ServiceFabricApplication -ApplicationName "fabric:/$appName" -ApplicationTypeName "$($appName)Type" -ApplicationTypeVersion $appVersion