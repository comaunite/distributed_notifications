# Distributed Notification System
A high-throughput, event-driven notification engine built with **.NET 10**, **RabbitMQ**, **PostgreSQL**, and **Redis**. This project demonstrates a scalable architecture for processing massive volumes of notifications across multiple delivery channels (Email, SMS, Push).

## 🚀 Workload Architecture
The system is designed around a "Fan-Out" pattern to ensure that the ingestion of a notification request is decoupled from the actual delivery logic.
1. **Notification API**: The entry point. It validates requests, persists the "Intent" into PostgreSQL, and publishes an event. Designed for low latency to handle high-burst traffic.
2. **Notification Orchestrator**: The "Brain." It consumes events from the main exchange, resolves user preferences (Email, SMS, and/or Push), and dispatches specific delivery tasks to specialized worker queues.
3. **Delivery Workers (Email, SMS, Push)**: Specialized consumers that handle the final mile of delivery. They are horizontally scalable to handle bottlenecks in external provider APIs.

## 🛠 Tech Stack & Nuances
### High-Performance Persistence
- **UUIDv7**: Primary keys use time-ordered UUIDs (`Guid.CreateVersion7()`) to ensure B-Tree index locality in PostgreSQL, significantly reducing page splits during high-concurrency inserts.
- **Context Pooling**: Leverages `AddDbContextPool` to minimize the overhead of creating/destroying instances per request. `DbContext`
- **Read-Write Splitting**: Support for separate Primary (Write) and Replica (Read) DbContexts to offload read-heavy preference lookups.

### Reliable Messaging
- **RabbitMQ Channel Pooling**: A custom `RabbitMqChannelPool` prevents "Channel Churn" by reusing AMQP channels across scoped requests.
- **Publisher Confirmations**: Ensures zero message loss by awaiting broker acknowledgments during the publish cycle.
- **Dead Letter Queues (DLQ)**: Every worker queue is configured with a Dead Letter Exchange (DLX). Poison messages are automatically moved to DLQs for manual inspection without blocking the pipeline.

## 🚦 Getting Started
Ensure you have Docker and .NET 10 SDK installed.

Then run:
```bash
docker-compose up --build
```
The API will be available at `http://localhost:8080/swagger` for testing. 

You can monitor the message flow via the RabbitMQ Management UI at `http://localhost:15672` (guest/guest).