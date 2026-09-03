# OSK Tech

Blazor Server + ASP.NET Core 8 + PostgreSQL + Redis. Регистрация, вход, личный кабинет с текстом, Outbox-синхронизация в Redis, выход со всех устройств, авто-разлогин после 24ч неактивности.

## Запуск

Инструкция для Windows: [docs/SETUP.md](docs/SETUP.md)

```bash
docker compose up -d
dotnet tool restore
dotnet run --project src/OskTech.Migrator
dotnet run --project src/OskTech.Host
```

PostgreSQL в Docker: порт **5433**. Приложение: **https://localhost:7079**

## Тесты

```bash
dotnet test
```

Integration-тесты используют Testcontainers (нужен Docker).

## Структура

```
src/OskTech.Domain/           — сущности
src/OskTech.Application/      — интерфейсы, options
src/OskTech.Infrastructure/   — EF Core, Redis, Outbox, сервисы
src/OskTech.Host/             — Blazor UI
src/OskTech.Migrator/         — миграции БД
docs/diagrams/                — архитектурные диаграммы
```
