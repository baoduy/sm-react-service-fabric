# Azure Service Fabric Support

The submodule to host React app on Service Fabric.

All stuffs in `service-fabric` folder are using for **[Azure Service Fabric](https://azure.microsoft.com/en-us/services/service-fabric/)** hosting purpose.
The project inside this folder will copy all files in dist folder and host as a static side in Azure Service Fabric.
I'm using .Net Core 2.0 to make the project is flexible enough to host on any platforms.

When build the Service Fabric application it will copy all files in `dist` folder to `wwwroot` folder. So ensure you run the `npm build` before deploy the Service Fabric app.

Defiantly, If you are not using **Azure Service Fabric**. This folder shall be deleted.

## SSL support

1. Import `localhost.pfx` with password is `localhost`
2. Run the application and see the result.

If you want to change to your SSL just simply update the thumbprint in the `ApplicationManifest.xml` in the bellow section

```xml
 <Certificates>
    <SecretsCertificate Name="SSL" X509FindType="FindByThumbprint" X509FindValue="[YOUR_THUMBPRINT]"/>
 </Certificates>
```
for more details please refer here [drunkcoding.net](http://drunkcoding.net/enable-https-endpoint-for-service-fabric-application/)