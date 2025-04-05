# Microservices with Hazelcast

## Overview

This project consists of four microservices that work together using dynamic service registration and distributed storage with Hazelcast:

- **FacadeService:**  
  Acts as the entry point for API requests, dynamically querying the ConfigServer to route requests to available LoggingService instances.
  
- **LoggingService:**  
  Stores and retrieves log messages using a Hazelcast Distributed Map. Multiple instances can run concurrently on different ports, and each instance registers itself with the ConfigServer.
  
- **MessagesService:**  
  Provides a static message response and may be extended for future message storage.
  
- **ConfigServer:**  
  Maintains a dynamic registry of service endpoints. LoggingService and MessagingService register on startup and unregister on shutdown. FacadeService queries this server to discover available service endpoints.

## Requirements

- .NET 7 or later
- Hazelcast .NET Client
- Docker (or an alternative method to run a Hazelcast member)
- Git
- A terminal or command prompt

## Installation

1. **Clone the repository:**

   ```bash
   git clone https://github.com/yourusername/microservices.git
   cd microservices
   ```

2. **Restore dependencies:**

   ```bash
   dotnet restore
   ```

3. **Install Hazelcast .NET Client package (if not already installed):**

   ```bash
   dotnet add package Hazelcast.Net
   ```

## Running the Services

Each service runs independently. You can use launch profiles or command-line arguments to run multiple instances of LoggingService.

### Start Hazelcast Member

For local testing, you can run a Hazelcast member using Docker:

```bash
docker run --network hazelcast-network -p 8080:8080 hazelcast/management-center
```

```bash
docker run -it --name hazelcast --network hazelcast-network -e HZ_CLUSTERNAME=dev -p 5701:5701 hazelcast/hazelcast
```

After it you can open the https://localhost:8080
There will be Hazelcast Management Center

When connecting cluster to Management Center you need to use name (in this example ```dev```) and adress in format - ```172.19.0.3:5701``` (without localhost)

### Start the Services (Windows)

To start all of the services you can use run.bat file

Commands for starting each project (in the Developer Command Prompt):

1. ConfigServer
   ```bash
   dotnet run --project ConfigServer
   ```

2. MessagesService
   ```bash
   start dotnet run --project MessagesService --MessagingServiceUrl "http://localhost:5002" --ConfigServerUrl "http://localhost:5001"
   ```

3. LoggingService (to run many instances just use this command several times with different LoggingServiceUrl property values)
   ```bash
   start dotnet run --project LoggingService --LoggingServiceUrl "http://localhost:5003" --ConfigServerUrl "http://localhost:5001"
   ```

4. FacadeService
   ```bash
   start dotnet run --project FacadeService
   ```

### Explanation how everything works

This project demonstrates a scalable microservices architecture using dynamic service registration and Hazelcast for distributed logging.

- **Dynamic Registration:**  
  When started, each instance of LoggingService and MessagingService registers its unique URL with the ConfigServer. When a service shuts down, it unregisters itself. This ensures that the ConfigServer always has an up-to-date list of active service endpoints.

- **Service Discovery:**  
  FacadeService queries the ConfigServer to obtain the list of available LoggingService endpoints. It then selects one at random (with retries if needed) to send log messages, ensuring load distribution and high availability.

- **Distributed Logging:**  
  LoggingService uses a Hazelcast Distributed Map to store log messages. This allows multiple LoggingService instances to share the same state and maintain consistency across the system.

Overall, the ConfigServer acts as the central registry, while FacadeService dynamically routes requests to available LoggingService instances based on real-time registration data.