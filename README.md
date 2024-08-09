# ax-h SSO

[https://sso.ax-h.com](sso.ax-h.com).

Basic OIDC provider based on Duende IdentityServer.

## DB

Requires dotnet EF tools installed.

```shell
dotnet tool install --global dotnet-ef
```

Note whenever an update to IdentityServer is installed, the migrations must be updated:

```shell
cd Sso
dotnet ef migrations add InitialPersistedGrant -c PersistedGrantDbContext -o Migrations/PersistedGrant
dotnet ef migrations add InitialConfiguration -c ConfigurationDbContext -o Migrations/Configuration
```

```shell
cd Sso
dotnet ef migrations add InitialSsoUser -c SsoDbContext -o Migrations/Sso
```
