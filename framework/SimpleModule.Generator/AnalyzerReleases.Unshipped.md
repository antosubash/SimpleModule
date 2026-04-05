### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SM0001 | SimpleModule.Generator | Error | Duplicate DbSet property name across modules
SM0002 | SimpleModule.Generator | Warning | Module has empty name
SM0003 | SimpleModule.Generator | Error | Multiple IdentityDbContext types found
SM0005 | SimpleModule.Generator | Error | IdentityDbContext has unexpected type arguments
SM0006 | SimpleModule.Generator | Warning | Entity configuration targets entity not in any DbSet
SM0007 | SimpleModule.Generator | Error | Duplicate entity configuration
SM0010 | SimpleModule.Generator | Error | Circular module dependency detected
SM0011 | SimpleModule.Generator | Error | Module directly references another module's implementation
SM0012 | SimpleModule.Generator | Warning | Contract interface has too many methods
SM0013 | SimpleModule.Generator | Error | Contract interface must be split
SM0014 | SimpleModule.Generator | Error | Referenced contracts assembly has no public interfaces
SM0025 | SimpleModule.Generator | Error | No implementation found for contract interface
SM0026 | SimpleModule.Generator | Error | Multiple implementations of contract interface
SM0027 | SimpleModule.Generator | Error | Permission field is not a const string
SM0028 | SimpleModule.Generator | Error | Contract implementation is not public
SM0029 | SimpleModule.Generator | Error | Contract implementation is abstract
SM0031 | SimpleModule.Generator | Warning | Permission value does not follow naming pattern
SM0032 | SimpleModule.Generator | Error | Permission class is not sealed
SM0033 | SimpleModule.Generator | Error | Duplicate permission value
SM0034 | SimpleModule.Generator | Warning | Permission value prefix does not match module name
SM0035 | SimpleModule.Generator | Warning | DTO type in contracts has no public properties
SM0038 | SimpleModule.Generator | Warning | Infrastructure type in Contracts assembly
SM0039 | SimpleModule.Generator | Warning | SaveChanges interceptor has transitive DbContext dependency
SM0015 | SimpleModule.Generator | Error | Duplicate view page name across modules
SM0040 | SimpleModule.Generator | Error | Duplicate module name
SM0041 | SimpleModule.Generator | Warning | View page name does not match module name prefix
SM0042 | SimpleModule.Generator | Error | Module has view endpoints but no ViewPrefix
SM0043 | SimpleModule.Generator | Warning | Module does not override any IModule methods
SM0044 | SimpleModule.Generator | Warning | Multiple IModuleOptions for same module
SM0045 | SimpleModule.Generator | Error | Feature class is not sealed
SM0046 | SimpleModule.Generator | Warning | Feature field naming violation
SM0047 | SimpleModule.Generator | Error | Duplicate feature name
SM0048 | SimpleModule.Generator | Error | Feature field is not a const string
SM0049 | SimpleModule.Generator | Error | Multiple endpoints in a single file
SM0052 | SimpleModule.Generator | Error | Module assembly name does not follow naming convention
SM0053 | SimpleModule.Generator | Error | Module has no matching Contracts assembly
