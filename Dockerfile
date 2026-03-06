FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY Directory.Build.props Directory.Packages.props ./
COPY *.slnx ./
COPY src/SimpleModule.Core/*.csproj src/SimpleModule.Core/
COPY src/SimpleModule.Database/*.csproj src/SimpleModule.Database/
COPY src/SimpleModule.Generator/*.csproj src/SimpleModule.Generator/
COPY src/SimpleModule.Api/*.csproj src/SimpleModule.Api/
COPY src/modules/Users/Users.Contracts/*.csproj src/modules/Users/Users.Contracts/
COPY src/modules/Users/Users/*.csproj src/modules/Users/Users/
COPY src/modules/Products/Products.Contracts/*.csproj src/modules/Products/Products.Contracts/
COPY src/modules/Products/Products/*.csproj src/modules/Products/Products/
COPY src/modules/Orders/Orders.Contracts/*.csproj src/modules/Orders/Orders.Contracts/
COPY src/modules/Orders/Orders/*.csproj src/modules/Orders/Orders/
RUN dotnet restore src/SimpleModule.Api/SimpleModule.Api.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish src/SimpleModule.Api/SimpleModule.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SimpleModule.Api.dll"]
