FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LoggingService/LoggingService.csproj", "LoggingService/"]
RUN dotnet restore "LoggingService/LoggingService.csproj"
COPY LoggingService/. "LoggingService/"
WORKDIR "/src/LoggingService"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LoggingService.dll"]