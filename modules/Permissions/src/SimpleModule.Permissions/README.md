# SimpleModule.Permissions

Permission management module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Role-based access control (RBAC) with database persistence
- Permission seeding for initial setup
- Cross-module authorization via contracts
- Permission assignment to roles
- Fine-grained endpoint authorization

## Installation

```bash
sm install SimpleModule.Permissions
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Permissions
```

## Defining Permissions in Your Own Modules

Any module — including modules **you** write in a downstream app — can contribute
permissions. Permission classes are auto-discovered by the SimpleModule source
generator: you do not need to call `AddPermissions<T>()` yourself.

The convention is one sealed class per module implementing `IModulePermissions`,
containing only `public const string` fields named `Module.Action`:

```csharp
using SimpleModule.Core.Authorization;

namespace MyApp.Customers;

public sealed class CustomersPermissions : IModulePermissions
{
    public const string View = "Customers.View";
    public const string Create = "Customers.Create";
    public const string Update = "Customers.Update";
    public const string Delete = "Customers.Delete";
}
```

Apply them on endpoints using the `RequirePermission` extension method or the
`[RequirePermission]` attribute:

```csharp
public sealed class CreateCustomerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", (CreateCustomerRequest request) => /* ... */)
            .RequirePermission(CustomersPermissions.Create);
}
```

The role-edit UI groups permissions by the prefix before the first dot
(`Customers.View` → "Customers" group), so your custom permissions appear
alongside framework permissions automatically.

If you scaffold modules with `sm new module <Name>`, a starter
`<Name>Permissions.cs` is generated for you.

See the [Permissions guide](https://github.com/antosubash/SimpleModule/blob/main/docs/site/guide/permissions.md)
for the full authorization model (claims transformation, wildcard matching,
testing patterns).

## Usage

The module is auto-discovered by the SimpleModule framework. Use
`IPermissionsContracts` to check permissions from other modules.

## License

MIT
