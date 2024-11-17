FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY WeatherApp.sln .

COPY WeatherApp.Web/WeatherApp.Web.csproj WeatherApp.Web/
COPY WeatherApp.Services/WeatherApp.Services.csproj WeatherApp.Services/
COPY WeatherApp.Core/WeatherApp.Core.csproj WeatherApp.Core/
COPY WeatherApp.Data/WeatherApp.Data.csproj WeatherApp.Data/
COPY WeatherApp.Tests/WeatherApp.Tests.csproj WeatherApp.Tests/

RUN dotnet restore

COPY . .
WORKDIR /src/WeatherApp.Web
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "WeatherApp.Web.dll"]
