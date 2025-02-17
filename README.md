# Microservices

## Overview
This project consists of three microservices:
- **FacadeService**: Acts as the entry point, handling API requests.
- **LoggingService**: Stores and retrieves log messages.
- **MessagesService**: Placeholder for future message storage.

## Requirements
- .NET 7 or later
- Git
- A terminal or command prompt

## Installation
1. **Clone the repository**
   ```sh
   git clone https://github.com/denhumen/microservices.git
   cd microservices
   ```
2. **Restore dependencies**
   ```sh
   dotnet restore
   ```

## ▶Running the Services
Each service runs independently. Open **three terminals** and run:

### Start `LoggingService`:
```sh
dotnet run --project LoggingService
```

### Start `MessagesService`:
```sh
dotnet run --project MessagesService
```

### Start `FacadeService`:
```sh
dotnet run --project FacadeService
```

## Testing the API
You can use **Postman, cURL, or an `.http` file**.

### **Send a message**
```http
POST http://localhost:5000/api/facade/send
Content-Type: application/json

{
    "message": "Hello from FacadeService!"
}
```

### **Fetch logs and messages**
```http
GET http://localhost:5000/api/facade/fetch
Accept: application/json
```

## Additional Features
- **Retry Mechanism** (If `LoggingService` is unavailable, retries 3 times).
- **Deduplication** (Prevents duplicate logs).
