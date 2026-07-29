# Build context is the repo root (see docker-compose.yml) because PizzaShop.Api references
# sibling projects (Application/Infrastructure/Domain) via relative ProjectReference paths.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy just the .csproj files first so `dotnet restore` is cached until a dependency actually
# changes, instead of busting on every source edit.
COPY src/PizzaShop.Domain/PizzaShop.Domain.csproj src/PizzaShop.Domain/
COPY src/PizzaShop.Application/PizzaShop.Application.csproj src/PizzaShop.Application/
COPY src/PizzaShop.Infrastructure/PizzaShop.Infrastructure.csproj src/PizzaShop.Infrastructure/
COPY src/PizzaShop.Api/PizzaShop.Api.csproj src/PizzaShop.Api/
RUN dotnet restore src/PizzaShop.Api/PizzaShop.Api.csproj

COPY src/ src/
RUN dotnet publish src/PizzaShop.Api/PizzaShop.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Program.cs applies EF Core migrations and seeds the SuperAdmin on startup (skipped only in
# the "Testing" environment), so no separate migration step is needed here.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PizzaShop.Api.dll"]
