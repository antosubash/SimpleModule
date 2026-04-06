using Microsoft.AspNetCore.Identity;

namespace SimpleModule.Host;

public partial class HostDbContext
{
    // Override SchemaVersion to opt into Identity Schema Version 3, which provisions the
    // AspNetUserPasskeys table required for WebAuthn/passkey authentication support.
    protected override Version SchemaVersion => IdentitySchemaVersions.Version3;
}
