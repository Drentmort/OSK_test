# Настройка окружения (Windows)

## Что уже сделано на машине

- Установлен **.NET SDK** (сборка проходит)
- Установлен **Docker Desktop** (нужен первый запуск)
- Исправлен Migrator: корректно читает `appsettings.json`
- PostgreSQL в Docker проброшен на порт **5433** (локальный PostgreSQL уже занимает **5432**)

## Шаг 1 — Запустить Docker Desktop

1. Пуск → **Docker Desktop**
2. Дождитесь статуса **Engine running** (зелёный индикатор)
3. При первом запуске может потребоваться:
   - включить **WSL 2** (`wsl --install` в PowerShell от администратора → перезагрузка)
   - принять лицензию Docker

Проверка в PowerShell:

```powershell
docker ps
```

Должен вернуть таблицу контейнеров без ошибки про `dockerDesktopLinuxEngine`.

## Шаг 2 — Поднять PostgreSQL и Redis

```powershell
cd D:\SKYROS\Repository\OSK_Tech\Drentmort\OSK_test
docker compose up -d
docker compose ps
```

Ожидаемые порты:

| Сервис     | Порт |
|------------|------|
| PostgreSQL | 5433 |
| Redis      | 6379 |

## Шаг 3 — Автоматическая настройка (рекомендуется)

```powershell
cd D:\SKYROS\Repository\OSK_Tech\Drentmort\OSK_test
powershell -ExecutionPolicy Bypass -File .\scripts\setup.ps1
```

Скрипт: `docker compose up`, ожидание PostgreSQL, `dotnet tool restore`, миграции.

## Шаг 4 — Запуск приложения

```powershell
dotnet dev-certs https --trust
dotnet run --project src/OskTech.Host
```

Открыть: **https://localhost:7079/register**

## Строки подключения (по умолчанию)

Файлы `src/OskTech.Host/appsettings.json` и `src/OskTech.Migrator/appsettings.json`:

```
PostgreSQL: Host=localhost;Port=5433;Database=osktech;Username=osk;Password=osk
Redis:      localhost:6379
```

## Если Docker не запускается

**Вариант A** — перезагрузка после установки Docker Desktop.

**Вариант B** — свой PostgreSQL на 5432: создайте БД и пользователя:

```sql
CREATE USER osk WITH PASSWORD 'osk';
CREATE DATABASE osktech OWNER osk;
```

И в `appsettings.json` укажите `Port=5432` и свои учётные данные. Redis всё равно нужен (Docker или [Memurai Developer](https://www.memurai.com/)).

## Проверка

```powershell
dotnet test tests/OskTech.UnitTests
dotnet test tests/OskTech.IntegrationTests   # нужен Docker
```
