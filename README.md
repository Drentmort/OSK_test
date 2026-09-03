# OSK Tech

Тестовое задание: Blazor Server + ASP.NET Core 8 + PostgreSQL + Redis.

## Структура

```
src/
  OskTech.Domain/           — сущности и доменная логика
  OskTech.Application/      — команды, запросы, интерфейсы
  OskTech.Infrastructure/   — EF Core, Redis, Outbox, Auth
  OskTech.Host/             — Blazor Server UI
  OskTech.Migrator/         — применение миграций БД
tests/
  OskTech.UnitTests/
  OskTech.IntegrationTests/
docs/diagrams/              — архитектурные диаграммы
```

## Требования

- .NET 8 SDK
- Docker (PostgreSQL, Redis)

## Запуск инфраструктуры

```bash
docker compose up -d
```

## Сборка

```bash
dotnet build OskTech.sln
```

## Диаграммы

См. [docs/diagrams](docs/diagrams/README.md).
