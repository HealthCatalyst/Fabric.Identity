![Build status](https://healthcatalyst.visualstudio.com/_apis/public/build/definitions/eaeb1198-1e3e-4938-88f1-918e8bf769af/328/badge)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/d1d11b48f7cc4fbb9277b4c1c12c2106)](https://www.codacy.com/app/HealthCatalyst/Fabric.Identity?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=HealthCatalyst/Fabric.Identity&amp;utm_campaign=Badge_Grade)
# Fabric.Identity

The Fabric.Identity service provides centralized authentication across the Fabric ecosystem. The goal is to allow client applications (aka relying party applications) the ability to offload authentication logic to Fabric.Identity so that developers of the client applications can concentrate on solving their core business needs and not have to build one off authentication systems. Fabric.Identity is based on the [OpenID Connect](http://openid.net/connect/) specification and leverages [IdentityServer4](http://identityserver.io/) as the OpenID Connect provider implemenation.

## Platform
The Fabric.Identity service is built using:

+ ASP .NET Core 1.1
+ [IdentityServer4](http://identityserver.io/)

## Getting Started

The documentation on our [Getting Started](https://github.com/HealthCatalyst/Fabric.Identity/wiki/Getting-Started) page will show you how to quickly get Fabric.Identity running in Docker and start developing an application using Fabric.Identity as your authentication mechanism.

## How to build and run
If you would like to build from source without using a Docker image follow the instructions below:

+ [Install .NET Core 1.1](https://www.microsoft.com/net/core#windowsvs2017)
+ Clone or download the repo
+ Launch a command prompt or powershell window and change directory to the Fabric.Identity.API directory and execute the following commands
  + `dotnet restore`
  + `dotnet run`

Fabric.Identity service will start up and listen on port 5001.

You can run the following curl commands to ensure the service is up and working properly:

```curl -G http://localhost:5001/.well-known/openid-configuration```

Which will return a json document representing the discovery information for the service:

```
{
  "issuer": "http://localhost:5001",
  "jwks_uri": "http://localhost:5001/.well-known/openid-configuration/jwks",
  "authorization_endpoint": "http://localhost:5001/connect/authorize",
  ...
}
```

You can then run the [`setup-samples.sh`](https://github.com/HealthCatalyst/Fabric.Identity/blob/master/Fabric.Identity.API/scripts/setup-samples.sh) script to setup the sample applications in Fabric.Identity.
