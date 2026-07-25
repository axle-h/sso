# ax-h SSO

[sso.ax-h.com](https://sso.ax-h.com).

Basic OIDC provider based on Duende IdentityServer.

## Configuration

Clients and users are config, not database rows. `appsettings.json` holds everything that
is not a secret; secrets come from the environment.

### Clients

Keyed by client id under `Clients`. Every client is a next-auth app of the same shape, so
only the origins are configured — the redirect, front channel logout and post logout URIs
are derived from them.

```json
{
  "Clients": {
    "risk": {
      "Name": "Risk With Friends",
      "Origins": ["https://risk.ax-h.com"],
      "Scopes": ["openid", "profile", "email", "roles", "read_users"]
    }
  }
}
```

Add a second origin to point a locally running client at this instance:

```json
"Origins": ["https://risk.ax-h.com", "http://localhost:3000"]
```

The shared secret is never committed:

```shell
Clients__risk__Secret=<secret>
```

### Users

There is no admin UI. `Users` is the source of truth for who exists and what roles they
hold, reconciled into the database on every startup. Roles are created on demand and are
emitted in the `roles` claim.

```json
{
  "Users": [
    {
      "Username": "alex",
      "Email": "alex@example.com",
      "FirstName": "Alex",
      "LastName": "Haslehurst",
      "Roles": ["make-money", "make-movies", "risk", "admin"]
    }
  ]
}
```

`Users__0__Password` is only used to create a user that does not exist yet. Once they
exist their password is theirs to change at `/ChangePassword` and editing config will not
reset it. Users in the database but not in config are left alone, not deleted.

### Local development

```shell
cd Sso
dotnet user-secrets set "Clients:risk:Secret" "<secret>"
dotnet user-secrets set "Users:0:Password" "<password>"
dotnet run
```

## DB

Only ASP.NET Identity and the IdentityServer operational store (grants, keys) are
persisted, to SQLite. Grants are persisted deliberately — dropping them would sign
everyone out of every client on each restart.

Requires dotnet EF tools installed.

```shell
dotnet tool install --global dotnet-ef
```

Note whenever an update to IdentityServer is installed, the operational store migrations
must be updated:

```shell
cd Sso
dotnet ef migrations add InitialPersistedGrant -c PersistedGrantDbContext -o Migrations/PersistedGrant
```

```shell
cd Sso
dotnet ef migrations add InitialSsoUser -c SsoDbContext -o Migrations/Sso
```
