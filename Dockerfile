# =============================================================================
# Stage 1: Base build image with .NET SDK + Node.js
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /src

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# =============================================================================
# Stage 2: .NET restore (cached unless .csproj / props files change)
# =============================================================================
FROM base AS restore
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY *.slnx ./

# Framework projects
COPY framework/SimpleModule.Core/*.csproj framework/SimpleModule.Core/
COPY framework/SimpleModule.Database/*.csproj framework/SimpleModule.Database/
COPY framework/SimpleModule.Generator/*.csproj framework/SimpleModule.Generator/
COPY framework/SimpleModule.Blazor/*.csproj framework/SimpleModule.Blazor/
COPY framework/SimpleModule.Hosting/*.csproj framework/SimpleModule.Hosting/
COPY framework/SimpleModule.DevTools/*.csproj framework/SimpleModule.DevTools/
COPY framework/SimpleModule.Storage/*.csproj framework/SimpleModule.Storage/
COPY framework/SimpleModule.Storage.Local/*.csproj framework/SimpleModule.Storage.Local/

# ServiceDefaults
COPY SimpleModule.ServiceDefaults/*.csproj SimpleModule.ServiceDefaults/

# Host
COPY template/SimpleModule.Host/*.csproj template/SimpleModule.Host/

# Modules (Contracts + Implementation)
COPY modules/Dashboard/src/SimpleModule.Dashboard.Contracts/*.csproj modules/Dashboard/src/SimpleModule.Dashboard.Contracts/
COPY modules/Dashboard/src/SimpleModule.Dashboard/*.csproj modules/Dashboard/src/SimpleModule.Dashboard/
COPY modules/Users/src/SimpleModule.Users.Contracts/*.csproj modules/Users/src/SimpleModule.Users.Contracts/
COPY modules/Users/src/SimpleModule.Users/*.csproj modules/Users/src/SimpleModule.Users/
COPY modules/OpenIddict/src/SimpleModule.OpenIddict.Contracts/*.csproj modules/OpenIddict/src/SimpleModule.OpenIddict.Contracts/
COPY modules/OpenIddict/src/SimpleModule.OpenIddict/*.csproj modules/OpenIddict/src/SimpleModule.OpenIddict/
COPY modules/Permissions/src/SimpleModule.Permissions.Contracts/*.csproj modules/Permissions/src/SimpleModule.Permissions.Contracts/
COPY modules/Permissions/src/SimpleModule.Permissions/*.csproj modules/Permissions/src/SimpleModule.Permissions/
COPY modules/Products/src/SimpleModule.Products.Contracts/*.csproj modules/Products/src/SimpleModule.Products.Contracts/
COPY modules/Products/src/SimpleModule.Products/*.csproj modules/Products/src/SimpleModule.Products/
COPY modules/Orders/src/SimpleModule.Orders.Contracts/*.csproj modules/Orders/src/SimpleModule.Orders.Contracts/
COPY modules/Orders/src/SimpleModule.Orders/*.csproj modules/Orders/src/SimpleModule.Orders/
COPY modules/Admin/src/SimpleModule.Admin.Contracts/*.csproj modules/Admin/src/SimpleModule.Admin.Contracts/
COPY modules/Admin/src/SimpleModule.Admin/*.csproj modules/Admin/src/SimpleModule.Admin/
COPY modules/PageBuilder/src/SimpleModule.PageBuilder.Contracts/*.csproj modules/PageBuilder/src/SimpleModule.PageBuilder.Contracts/
COPY modules/PageBuilder/src/SimpleModule.PageBuilder/*.csproj modules/PageBuilder/src/SimpleModule.PageBuilder/
COPY modules/Settings/src/SimpleModule.Settings.Contracts/*.csproj modules/Settings/src/SimpleModule.Settings.Contracts/
COPY modules/Settings/src/SimpleModule.Settings/*.csproj modules/Settings/src/SimpleModule.Settings/
COPY modules/AuditLogs/src/SimpleModule.AuditLogs.Contracts/*.csproj modules/AuditLogs/src/SimpleModule.AuditLogs.Contracts/
COPY modules/AuditLogs/src/SimpleModule.AuditLogs/*.csproj modules/AuditLogs/src/SimpleModule.AuditLogs/
COPY modules/Marketplace/src/SimpleModule.Marketplace.Contracts/*.csproj modules/Marketplace/src/SimpleModule.Marketplace.Contracts/
COPY modules/Marketplace/src/SimpleModule.Marketplace/*.csproj modules/Marketplace/src/SimpleModule.Marketplace/
COPY modules/FileStorage/src/SimpleModule.FileStorage.Contracts/*.csproj modules/FileStorage/src/SimpleModule.FileStorage.Contracts/
COPY modules/FileStorage/src/SimpleModule.FileStorage/*.csproj modules/FileStorage/src/SimpleModule.FileStorage/
COPY modules/FeatureFlags/src/SimpleModule.FeatureFlags.Contracts/*.csproj modules/FeatureFlags/src/SimpleModule.FeatureFlags.Contracts/
COPY modules/FeatureFlags/src/SimpleModule.FeatureFlags/*.csproj modules/FeatureFlags/src/SimpleModule.FeatureFlags/
COPY modules/Tenants/src/SimpleModule.Tenants.Contracts/*.csproj modules/Tenants/src/SimpleModule.Tenants.Contracts/
COPY modules/Tenants/src/SimpleModule.Tenants/*.csproj modules/Tenants/src/SimpleModule.Tenants/

RUN dotnet restore template/SimpleModule.Host/SimpleModule.Host.csproj

# =============================================================================
# Stage 3: npm restore (cached unless package.json / lockfile changes)
# =============================================================================
FROM restore AS npm-restore
WORKDIR /src

# Root manifest + lockfile
COPY package.json package-lock.json ./

# Workspace package.json files (must exist for npm ci with workspaces)
COPY modules/Admin/src/SimpleModule.Admin/package.json modules/Admin/src/SimpleModule.Admin/
COPY modules/AuditLogs/src/SimpleModule.AuditLogs/package.json modules/AuditLogs/src/SimpleModule.AuditLogs/
COPY modules/Dashboard/src/SimpleModule.Dashboard/package.json modules/Dashboard/src/SimpleModule.Dashboard/
COPY modules/FeatureFlags/src/SimpleModule.FeatureFlags/package.json modules/FeatureFlags/src/SimpleModule.FeatureFlags/
COPY modules/FileStorage/src/SimpleModule.FileStorage/package.json modules/FileStorage/src/SimpleModule.FileStorage/
COPY modules/Marketplace/src/SimpleModule.Marketplace/package.json modules/Marketplace/src/SimpleModule.Marketplace/
COPY modules/OpenIddict/src/SimpleModule.OpenIddict/package.json modules/OpenIddict/src/SimpleModule.OpenIddict/
COPY modules/Orders/src/SimpleModule.Orders/package.json modules/Orders/src/SimpleModule.Orders/
COPY modules/PageBuilder/src/SimpleModule.PageBuilder/package.json modules/PageBuilder/src/SimpleModule.PageBuilder/
COPY modules/Products/src/SimpleModule.Products/package.json modules/Products/src/SimpleModule.Products/
COPY modules/Settings/src/SimpleModule.Settings/package.json modules/Settings/src/SimpleModule.Settings/
COPY modules/Tenants/src/SimpleModule.Tenants/package.json modules/Tenants/src/SimpleModule.Tenants/
COPY modules/Users/src/SimpleModule.Users/package.json modules/Users/src/SimpleModule.Users/
COPY packages/SimpleModule.Client/package.json packages/SimpleModule.Client/
COPY packages/SimpleModule.Theme.Default/package.json packages/SimpleModule.Theme.Default/
COPY packages/SimpleModule.UI/package.json packages/SimpleModule.UI/
COPY template/SimpleModule.Host/ClientApp/package.json template/SimpleModule.Host/ClientApp/
COPY tests/e2e/package.json tests/e2e/
COPY docs/site/package.json docs/site/
COPY website/package.json website/

RUN npm ci

# =============================================================================
# Stage 4: Frontend build + .NET publish
# =============================================================================
FROM npm-restore AS build
WORKDIR /src

COPY . .

# Build all frontend workspaces
RUN npm run build

# Publish the .NET application (MSBuild targets detect existing frontend
# outputs and only run TailwindBuild which needs Blazor SSR content)
RUN dotnet publish template/SimpleModule.Host/SimpleModule.Host.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:ErrorOnDuplicatePublishOutputFiles=false

# =============================================================================
# Stage 5: Runtime (slim image, non-root user)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && apt-get clean && rm -rf /var/lib/apt/lists/* \
    && groupadd --system --gid 1001 appgroup \
    && useradd --system --uid 1001 --gid appgroup --create-home appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "SimpleModule.Host.dll"]
