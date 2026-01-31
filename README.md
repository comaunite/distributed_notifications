# Distributed Notification System
A high-throughput, event-driven notification engine built with **.NET 10**, **RabbitMQ**, **PostgreSQL**, and **Redis**.
This project demonstrates a scalable architecture for processing massive volumes of notifications across multiple delivery channels (Email, SMS, Push).

Note: I skipped the NoSQL database for simplicity's sake, but it can be easily integrated for storing notification history or analytics.

## 🚀 Workload Architecture
The system is designed around a "Fan-Out" pattern to ensure that the ingestion of a notification request is decoupled from the actual delivery logic.
1. **Notification API**: The entry point. It validates requests, persists the "Intent" into PostgreSQL, and publishes an event. Designed for low latency to handle high-burst traffic.
2. **Notification Orchestrator**: The "Brain." It consumes events from the main exchange, resolves user preferences (Email, SMS, and/or Push) using a **producer-consumer pattern** with bounded channels, and dispatches specific delivery tasks to specialized worker queues in parallel across 16 concurrent consumers.
3. **Delivery Workers (Email, SMS, Push)**: Specialized consumers that handle the final mile of delivery. They are horizontally scalable to handle bottlenecks in external provider APIs.

## 🛠 Tech Stack & Nuances
### High-Performance Persistence
- **UUIDv7**: Primary keys use time-ordered UUIDs (`Guid.CreateVersion7()`) to ensure B-Tree index locality in PostgreSQL, significantly reducing page splits during high-concurrency inserts.
- **Read-Write Splitting**: Support for separate Primary (Write) and Replica (Read) DbContexts to offload read-heavy preference lookups.
- **Dapper Integration**: For performance-critical queries, Dapper is used alongside EF Core to minimize overhead.
- **Producer-Consumer Pattern**: Uses `System.Threading.Channels` to decouple recipient resolution (producer) from message publishing (consumers), enabling backpressure handling and efficient memory usage.
- **Parallel Processing**: Spawns 16 concurrent consumer tasks, each with its own scoped `OrchestrationHandler` and pooled RabbitMQ channel, to maximize throughput during fan-out operations.
- **Thread-Safe Counters**: Tracks processing statistics using `Interlocked` operations to safely increment counters across parallel consumer tasks without locks.
- **Template Caching**: Pre-renders notification templates once per orchestration and shares them across all consumers to avoid redundant serialization overhead.

### Caching Layer
- **Redis Caching**: User notification preferences are cached in Redis with a TTL to reduce database load. Cache invalidation occurs on preference updates.
- **Cache-Aside Pattern**: The orchestrator first checks Redis for preferences before querying PostgreSQL.
- **Connection Pooling**: Uses `StackExchange.Redis` with optimized connection pooling settings for high-throughput scenarios.

### Reliable Messaging
- **RabbitMQ Channel Pooling**: A custom `RabbitMqChannelPool` prevents "Channel Churn" by reusing AMQP channels across scoped requests, with each consumer task renting its own channel for the duration of processing.
- **Publisher Confirmations**: Ensures zero message loss by awaiting broker acknowledgments during the publish cycle.
- **Dead Letter Queues (DLQ)**: Every worker queue is configured with a Dead Letter Exchange (DLX). Poison messages are automatically moved to DLQs for manual inspection without blocking the pipeline.
- **Graceful Cancellation**: Uses linked `CancellationTokenSource` to coordinate shutdown across producer and consumer tasks, ensuring clean resource disposal.

## 🚦 Getting Started
Ensure you have Docker and .NET 10 SDK installed.

Then run:
```bash
docker-compose up --build
```
Optionally, run the tools/TestDataPopulator to seed test data (1mil users, with default preferences and 200000 of custom preferences):
```bash
dotnet run --project tools/TestDataPopulator/TestDataPopulator.csproj
```

The API to produce notifications will be available at `http://localhost:8080/swagger` 

You can monitor the message flow via the RabbitMQ Management UI at `http://localhost:15672` (guest/guest).