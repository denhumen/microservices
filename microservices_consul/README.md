# Microservices with Hazelcast

## Overview

This project consists of three microservices that work together using dynamic service registration, distributed storage with Hazelcast and producing/consuming :

- **FacadeService:**  
  API gateway that discovers services via Consul, routes log writes to a random LoggingService, and produces messages to Kafka. Also reads logs & messages.
  
- **LoggingService:**  
  Stores and retrieves log messages using a Hazelcast Distributed Map. Multiple instances can run concurrently on different ports, and each instance registers itself with the Consul.
  
- **MessagesService:**  
  Consumes messages from Kafka. Multiple instances for high availability.

## Requirements

- .NET 7 or later
- Hazelcast .NET Client
- Kafka .NET Client
- Consul .NET Client
- Docker
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

4. **Install Kafka .NET Client package (if not already installed):**

   ```bash
   dotnet add package Confluent.Kafka
   ```

5. **Install Consul .NET Client package (if not already installed):**

   ```bash
   dotnet add package Consul
   ```

## Running the Services

Run following command in root directory:

```bash
docker-compose up --build -d
```

When you run all of the services via docker-compose up command all of services will exit with error. That’s because the KV properties in consul are not set

So after they fail, run following commands one by one in terminal:

```bash
docker compose exec consul sh

consul kv put config/hazelcast/ClusterName dev
consul kv put config/hazelcast/Networking/Addresses/0 hazelcast:5701

consul kv put config/messaging/BootstrapServers "kafka-1:9092,kafka-2:9092,kafka-3:9092"
consul kv put config/messaging/Topic messages

exit
```

Each service runs independently in Docker, so you can see logs there.

### Explanation how everything works

This project demonstrates a scalable microservices architecture using dynamic service registration and Hazelcast for distributed logging.

- **Dynamic Registration:**  
  When started, each instance of LoggingService and MessagingService registers its unique URL with the Consul. When a service shuts down, it unregisters itself. This ensures that the Consul always has an up-to-date list of active service endpoints.

- **Service Discovery:**  
  FacadeService queries the Consul to obtain the list of available LoggingService endpoints. It then selects one at random (with retries if needed) to send log messages, ensuring load distribution and high availability.

- **Distributed Logging:**  
  LoggingService uses a Hazelcast Distributed Map to store log messages. This allows multiple LoggingService instances to share the same state and maintain consistency across the system.

- **Message Queuing with Kafka:**  
  FacadeService serializes each incoming request to JSON and pushes it onto a Kafka topic. MessagesService instances, each running in their own consumer group, subscribe to that topic—so every message is reliably queued and distributed to all active consumers.

- **Fault Tolerance via Replication:**  
  Your Kafka topic is created with a replication factor ≥ 2, and Hazelcast runs as a cluster. If any single Kafka broker or Hazelcast member goes down, a replica automatically takes over—ensuring no data loss and uninterrupted service.  

Overall, the Consul acts as the central registry, while FacadeService dynamically routes requests to available LoggingService instances and produces messages to Kafka MessageQueue based on real-time registration data. At that time MessagesService instances read messages from Kafka MessageQueue.