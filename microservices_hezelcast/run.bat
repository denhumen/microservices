@echo off
echo Starting ConfigServer...
start dotnet run --project ConfigServer
timeout /t 3

echo Starting MessagingService...
start dotnet run --project MessagesService --MessagingServiceUrl "http://localhost:5002" --ConfigServerUrl "http://localhost:5001"
timeout /t 3

echo Starting LoggiangService on port 5001...
start dotnet run --project LoggingService --LoggingServiceUrl "http://localhost:5003" --ConfigServerUrl "http://localhost:5001"
timeout /t 3

echo Starting LoggingService on port 5004...
start dotnet run --project LoggingService --LoggingServiceUrl "http://localhost:5004" --ConfigServerUrl "http://localhost:5001"
timeout /t 3

echo Starting LoggingService on port 5005...
start dotnet run --project LoggingService  --LoggingServiceUrl "http://localhost:5005" --ConfigServerUrl "http://localhost:5001"
timeout /t 3

echo Starting FacadeService...
start dotnet run --project FacadeService
echo All services started.
pause