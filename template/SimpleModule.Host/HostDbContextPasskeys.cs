// Passkey support is enabled by configuring IdentityOptions.Stores.SchemaVersion
// in UsersModule.ConfigureServices. The source-generated HostDbContext inherits from
// IdentityDbContext, which reads SchemaVersion from IdentityOptions at model creation time
// and adds the AspNetUserPasskeys table when Version3 is active.
//
// This file is a placeholder for any future hand-written HostDbContext extensions
// related to passkey infrastructure (e.g. custom entity configurations).

namespace SimpleModule.Host;

public partial class HostDbContext;
