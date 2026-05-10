# CmsImporter

Take-home solution for below task:

**Provide a facility for customers to import their content from an existing WCMS into a new one, then notify upstream systems that the new content is available.**

**Architectural approach:** Clean Architecture layered solution with a streaming producer-consumer pipeline. Content is extracted lazily via `IAsyncEnumerable<T>`, transformed and validated in parallel, funnelled through bounded `Channel<T>` with backpressure, batch-upserted into PostgreSQL, and published as events using RabbitMQ without ever holding the full dataset in memory.

---

## Architecture

### Layer diagram

```mermaid
graph TD
    subgraph Core["CmsImporter.Core - Domain"]
        E["Entities · ValueObjects · DTOs"]
        I["IContentRepository · ISourceConnector<br/>IEventPublisher · IImportProgressTracker"]
    end

    subgraph App["CmsImporter.Application - Orchestration"]
        PIPE["Pipeline stages<br/>Extract → Transform → Validate → Load → Notify"]
        QS["ContentQueryService (IQueryable composition)"]
        REG["SourceConnectorRegistry (adapter lookup)"]
    end

    subgraph Infra["CmsImporter.Infrastructure - I/O plugins"]
        DB["EfContentRepository (EF Core + Postgres JSONB)"]
        MQ["RabbitMqEventPublisher (async v7 API)"]
        CON["FileSystemJsonSourceConnector<br/>HttpRestSourceConnector"]
        RES["Polly ResiliencePipelines"]
    end

    subgraph Api["CmsImporter.WebApi - Composition"]
        EP["Minimal API endpoints"]
        WK["ImportWorker (BackgroundService)"]
        CHAN["Channel&lt;ImportJob&gt; (bounded, Wait)"]
    end

    Core --> App
    Core --> Infra
    App  --> Infra
    Infra --> Api
    App  --> Api

    classDef layer fill:#f8fafc,stroke:#64748b,color:#0f172a
    class Core,App,Infra,Api layer
```

Layer dependencies flow strictly inward. `Core` has zero external dependencies. `Infrastructure` implements `Core` interfaces as plugins. Adding future connectors does not touch any other layer.

### Data flow / pipeline

```mermaid
flowchart TB
    subgraph SOURCES["Source CMS adapters (ISourceConnector)"]
        FS["FileSystemJsonSourceConnector<br/>JsonSerializer.DeserializeAsyncEnumerable"]
        HTTP["HttpRestSourceConnector<br/>IHttpClientFactory + paginated GET"]
    end

    subgraph WEBAPI["CmsImporter.WebApi"]
        API["POST /imports<br/>GET /imports/{id}<br/>GET /content (IQueryable)"]
        JOBS["Channel&lt;ImportJob&gt;<br/>(bounded, FullMode=Wait)"]
        WORKER["ImportWorker<br/>(BackgroundService)"]
        API -->|"WriteAsync"| JOBS
        JOBS -->|"ReadAllAsync"| WORKER
    end

    subgraph PIPELINE["ImportOrchestrator (per-job DI scope)"]
        EXTRACT["Extract<br/>IAsyncEnumerable&lt;RawContent&gt;"]
        TFV["Transform + Validate<br/>(Parallel.ForEachAsync)"]
        ITEM_CHAN["Channel&lt;ContentItem&gt;<br/>(bounded backpressure)"]
        DEDUP["Deduplication<br/>ConcurrentDictionary"]
        LOAD["Load<br/>EF Core upsert<br/>+ ChangeTracker.Clear"]
        NOTIFY["Notify<br/>RabbitMQ topic publish"]

        EXTRACT --> TFV
        TFV -->|"WriteAsync"| ITEM_CHAN
        ITEM_CHAN -->|"ReadAllAsync"| DEDUP
        DEDUP -->|"batch (n=200)"| LOAD
        LOAD --> NOTIFY
    end

    subgraph SINKS["Sinks"]
        PG[("PostgreSQL 16<br/>JSONB Body + Metadata<br/>concurrency token")]
        RMQ["RabbitMQ 3 topic exchange<br/>cms.content.imported.{src}.{type}"]
        SUB["SampleSubscriber<br/>(prints events)"]
    end

    SOURCES --> EXTRACT
    WORKER -->|"per-job scope"| PIPELINE
    LOAD --> PG
    NOTIFY --> RMQ
    RMQ --> SUB

    classDef channel fill:#fef3c7,stroke:#92400e,color:#0f172a
    classDef parallel fill:#ddd6fe,stroke:#5b21b6,color:#0f172a
    classDef sink fill:#bbf7d0,stroke:#15803d,color:#0f172a
    class JOBS,ITEM_CHAN channel
    class TFV parallel
    class PG,RMQ sink
```

**Two channels** sit on the data path: `Channel<ImportJob>` decouples the API from the background worker; `Channel<ContentItem>` gives the parallel transform fan-out backpressure against the single-reader load consumer. Both are bounded with `FullMode.Wait` — slow downstream genuinely throttles fast upstream instead of buffering unbounded in memory.

---

## Quick start

**Prerequisites:** .NET 9 SDK · Docker Desktop

```powershell
# 1. Start Postgres + RabbitMQ (Postgres on host port 5433)
docker compose up -d

# 2. Start the API — migrations apply automatically
dotnet run --project src/CmsImporter.WebApi
# → Scalar API explorer: http://localhost:5050/scalar/v1
# → Health check:        http://localhost:5050/health

# 3. Recommended: run the interactive demo console
#    (menu-driven — no curl/PowerShell needed)
dotnet run --project src/CmsImporter.DemoConsole

# 4. Optional: open a second terminal to watch RabbitMQ events in real time
dotnet run --project src/CmsImporter.SampleSubscriber
```

<details>
<summary>Direct API calls with Invoke-RestMethod</summary>

```powershell
# Import from the bundled JSON sample (FileSystem connector)
Invoke-RestMethod -Method Post http://localhost:5050/imports `
  -ContentType "application/json" `
  -Body '{
    "source": "FileSystem",
    "config": {
      "path": "C:/Projects/CmsImporter/samples/source-cms",
      "sourceSystem": "demo-files"
    }
  }'

# Import from the dev mock paginated REST endpoint (HttpRest connector)
Invoke-RestMethod -Method Post http://localhost:5050/imports `
  -ContentType "application/json" `
  -Body '{
    "source": "HttpRest",
    "config": {
      "baseUrl":      "http://localhost:5050/demo/source-feed",
      "sourceSystem": "demo-rest",
      "pageSize":     "2"
    }
  }'

# Re-run the same import — SampleSubscriber will show IsNew=false (upsert path)

# Query imported content (IQueryable composition — single SQL, deferred execution)
Invoke-RestMethod "http://localhost:5050/content?sourceSystem=demo-files&type=Article"
```

</details>

### Run the tests

```powershell
# Fast unit tests only — no Docker required
dotnet test --filter "FullyQualifiedName!~IntegrationTests"

# Full suite including Testcontainers (Postgres 16 + RabbitMQ 3.13) end-to-end
dotnet test
```

---

## Project structure

```
CmsImporter.sln
├── global.json                    # SDK pinned to .NET 9.0.306
├── Directory.Build.props          # TreatWarningsAsErrors, C# latest, Nullable enabled
├── docker-compose.yml             # postgres:16-alpine + rabbitmq:3.13-management-alpine
├── .github/workflows/ci.yml       # build → unit tests
├── samples/source-cms/
│   └── site-export.json           # 5 sample items (2 Pages, 2 Articles, 1 Media)
└── src/
    ├── CmsImporter.Core/          # Pure domain — entities, value objects, abstractions
    ├── CmsImporter.Application/   # Pipeline stages, orchestrator, query service
    ├── CmsImporter.Infrastructure/ # EF Core, RabbitMQ, source connectors, Polly
    ├── CmsImporter.WebApi/        # Minimal API + ImportWorker (BackgroundService)
    ├── CmsImporter.SampleSubscriber/ # Console RabbitMQ subscriber (proves notifications)
    └── CmsImporter.DemoConsole/   # Interactive menu-driven demo

tests/
├── CmsImporter.Application.Tests/ # unit tests — stages, orchestrator, progress
├── CmsImporter.Infrastructure.Tests/ # unit tests — registry, FileSystem connector
└── CmsImporter.IntegrationTests/  # end-to-end tests (Testcontainers)
```

**Layer dependencies** are strict: `Core` has no dependencies; `Application` depends on `Core`; `Infrastructure` depends on `Application` + `Core`; `WebApi` wires everything up. Source connectors and the RabbitMQ publisher are **plugins** behind `Core` interfaces. — adding a new source CMS doesn't touch any other layer.

---

## Tech stack

- **.NET 9** (C# 13) — `global.json` pins the SDK version
- **PostgreSQL 16 (alpine)** + **RabbitMQ 3.13 management (alpine)** — via `docker-compose.yml`
- **EF Core 9** + **Npgsql.EntityFrameworkCore.PostgreSQL 9** — JSONB-aware mapping, optimistic concurrency
- **RabbitMQ.Client 7** — async v7 API (`CreateConnectionAsync`, `BasicPublishAsync`, `AsyncEventingBasicConsumer`)
- **Polly v8** + **Microsoft.Extensions.Http.Resilience** — retry, circuit-breaker, exponential backoff with jitter
- **Serilog** — console + daily rolling file sinks; structured enrichment with `JobId`, `SourceSystem`, stage timings
- **OpenTelemetry** — ASP.NET Core, HttpClient, EF Core, and custom `ImportActivitySource`; console exporter in dev, OTLP-ready
- **NUnit 4** + **NSubstitute 5** + **Testcontainers 4** for tests
