# Структура проектов

```mermaid
flowchart TB
    subgraph solution [OskTech.sln]
        subgraph presentation [Presentation]
            Host["OskTech.Host\nASP.NET Core + Blazor Server"]
        end

        subgraph application [Application]
            App["OskTech.Application\nCommands, Queries, Interfaces, DTOs"]
        end

        subgraph domain [Domain]
            Dom["OskTech.Domain\nEntities, ValueObjects, DomainEvents"]
        end

        subgraph infrastructure [Infrastructure]
            Infra["OskTech.Infrastructure\nEF Core, Redis, Outbox, Auth"]
        end

        subgraph tools [Tools]
            Migrator["OskTech.Migrator\ndotnet run -- migrate"]
        end

        subgraph tests [Tests]
            UnitTests["OskTech.UnitTests"]
            IntTests["OskTech.IntegrationTests\nTestcontainers: PG + Redis"]
        end
    end

    Host --> App
    Host --> Infra
    App --> Dom
    Infra --> App
    Infra --> Dom
    Migrator --> Infra
    UnitTests --> App
    IntTests --> Host
```

## Дерево каталогов

```
OSK_test/
├── OskTech.sln
├── docker-compose.yml
├── Directory.Build.props
├── src/
│   ├── OskTech.Domain/
│   │   └── Entities/
│   ├── OskTech.Application/
│   │   └── Interfaces/
│   ├── OskTech.Infrastructure/
│   │   └── Persistence/
│   ├── OskTech.Host/
│   │   ├── Components/
│   │   ├── Middleware/
│   │   └── Program.cs
│   └── OskTech.Migrator/
│       └── Program.cs
├── tests/
│   ├── OskTech.UnitTests/
│   └── OskTech.IntegrationTests/
└── docs/
    └── diagrams/
```

## Зависимости слоёв

```
Domain ← Application ← Infrastructure ← Host
                              ↑
                         Migrator
```
