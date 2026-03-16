FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY *.slnx ./
COPY framework/SimpleModule.Core/*.csproj framework/SimpleModule.Core/
COPY framework/SimpleModule.Database/*.csproj framework/SimpleModule.Database/
COPY framework/SimpleModule.Generator/*.csproj framework/SimpleModule.Generator/
COPY framework/SimpleModule.Blazor/*.csproj framework/SimpleModule.Blazor/
COPY template/SimpleModule.Host/*.csproj template/SimpleModule.Host/
COPY modules/Dashboard/src/Dashboard/*.csproj modules/Dashboard/src/Dashboard/
COPY modules/Users/src/Users.Contracts/*.csproj modules/Users/src/Users.Contracts/
COPY modules/Users/src/Users/*.csproj modules/Users/src/Users/
COPY modules/Products/src/Products.Contracts/*.csproj modules/Products/src/Products.Contracts/
COPY modules/Products/src/Products/*.csproj modules/Products/src/Products/
COPY modules/Orders/src/Orders.Contracts/*.csproj modules/Orders/src/Orders.Contracts/
COPY modules/Orders/src/Orders/*.csproj modules/Orders/src/Orders/
RUN dotnet restore template/SimpleModule.Host/SimpleModule.Host.csproj

COPY . .
RUN dotnet publish template/SimpleModule.Host/SimpleModule.Host.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SimpleModule.Host.dll"]
