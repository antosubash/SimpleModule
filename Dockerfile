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
# Stage 3: Frontend build + .NET publish
# =============================================================================
FROM restore AS build
WORKDIR /src

COPY . .

# Download Tailwind CSS CLI for Linux
RUN ARCH=$(uname -m) && \
    case "$ARCH" in x86_64) ARCH="x64" ;; aarch64|arm64) ARCH="arm64" ;; esac && \
    curl -sL "https://github.com/tailwindlabs/tailwindcss/releases/download/v4.1.3/tailwindcss-linux-${ARCH}" -o tools/tailwindcss && \
    chmod +x tools/tailwindcss

# Install npm dependencies and build all frontend workspaces
RUN npm ci
RUN npm run build

# Publish the .NET application (MSBuild targets detect existing frontend
# outputs and only run TailwindBuild which needs Blazor SSR content)
RUN dotnet publish template/SimpleModule.Host/SimpleModule.Host.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:ErrorOnDuplicatePublishOutputFiles=false

# =============================================================================
# Stage 4: Runtime (slim image, non-root user)
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
