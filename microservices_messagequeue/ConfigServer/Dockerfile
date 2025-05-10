FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ConfigServer/ConfigServer.csproj", "ConfigServer/"]
RUN dotnet restore "ConfigServer/ConfigServer.csproj"
COPY ConfigServer/. "ConfigServer/"
WORKDIR "/src/ConfigServer"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConfigServer.dll"]