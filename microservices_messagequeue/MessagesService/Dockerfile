FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MessagesService/MessagesService.csproj", "MessagesService/"]
RUN dotnet restore "MessagesService/MessagesService.csproj"
COPY MessagesService/. "MessagesService/"
WORKDIR "/src/MessagesService"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MessagesService.dll"]