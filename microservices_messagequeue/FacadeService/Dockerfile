FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FacadeService/FacadeService.csproj", "FacadeService/"]
RUN dotnet restore "FacadeService/FacadeService.csproj"
COPY FacadeService/. "FacadeService/"
WORKDIR "/src/FacadeService"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FacadeService.dll"]