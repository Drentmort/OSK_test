# Архитектура системы

```mermaid
flowchart LR
    subgraph client [Client Browser]
        BlazorUI["Blazor Server UI\nRegister / Login / Cabinet"]
    end

    subgraph host [OskTech.Host]
        Middleware["Middleware\nAuth, Activity, RateLimit"]
        MediatR["MediatR Handlers"]
        BG1["OutboxProcessor\nHostedService"]
        BG2["InactivityChecker\nHostedService every 5min"]
    end

    subgraph data [Data Layer]
        PG[(PostgreSQL\nusers, user_texts,\nrefresh_tokens, outbox)]
        Redis[(Redis\ncache, sessions,\nrate limits, blacklist)]
    end

    BlazorUI <-->|SignalR + HTTP| Middleware
    Middleware --> MediatR
    MediatR -->|"Write: TX + Outbox"| PG
    MediatR -->|"Read: cache-first"| Redis
    BG1 -->|"Poll outbox"| PG
    BG1 -->|"Sync cache"| Redis
    BG2 -->|"Find inactive >24h"| PG
    BG2 -->|"Revoke sessions"| Redis
    MediatR -.->|"cache miss"| PG
```

## Потоки данных

| Операция | PostgreSQL | Redis | Outbox |
|----------|------------|-------|--------|
| Register/Login | users, refresh_tokens | session, rate limit | — |
| Get text | user_texts (on miss) | cache:text:{userId} | — |
| Save text | user_texts + outbox row | — (async) | UserTextUpdated |
| Logout all | revoke tokens + outbox | del sessions + blacklist | SessionsRevoked |
| Activity touch | LastActivityAt + outbox | session TTL refresh | ActivityUpdated |

## Outbox

1. В одной транзакции EF Core: UPDATE `user_texts` + INSERT `outbox_messages`.
2. `OutboxProcessorHostedService` читает необработанные записи (`FOR UPDATE SKIP LOCKED`).
3. Пишет в Redis, помечает `ProcessedAt`.
4. При сбое — retry с exponential backoff.

## Нагрузка (10k concurrent)

- Kestrel: `MaxConcurrentConnections = 10000`
- Npgsql pool: `Maximum Pool Size=200`
- Redis: один `IConnectionMultiplexer`
- Rate limit: login/register через Redis sliding window
- `CancellationToken` во всех handler'ах
