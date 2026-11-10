# Руководство по DoctorRegistry

## Назначение

DoctorRegistry хранит единую предметную сущность `Practitioner`. API предоставляет JSON CRUD и FHIR R5 endpoints. Worker принимает import-сообщения RabbitMQ и применяет их к PostgreSQL.

## Архитектура

- `Domain` содержит Practitioner и его инварианты.
- `Application` содержит use cases, DTO, валидацию, import handler и persistence contracts.
- `Infrastructure` содержит один EF Core `DoctorRegistryDbContext`, PostgreSQL migrations и RabbitMQ connection/topology/publisher.
- `Api` — Minimal API; Domain entities наружу не возвращаются.
- `Worker` — BackgroundService с RabbitMQ consumer.

MediatR и Generic Repository не используются. Redis и Outbox Pattern пока не реализованы.

## Потоки и транзакции

HTTP endpoint вызывает Application handler, который валидирует запрос и обращается к PostgreSQL через persistence contract. Domain methods сохраняют инварианты.

Import открывает одну PostgreSQL transaction, атомарно регистрирует MessageId через `INSERT ... ON CONFLICT DO NOTHING`, затем создаёт или обновляет Practitioner и commit-ит обе операции. Повторный MessageId — безопасный no-op.

Worker использует manual ACK, prefetch и отдельный DI scope на delivery. Permanent errors идут в DLQ, transient errors проходят retry queues с TTL 30 секунд, 5 минут и 30 минут. ACK выполняется после commit или подтверждённой публикации replacement-сообщения. При сбое channel consumer пересоздаёт topology, channel, QoS и subscription.

## FHIR

FHIR DTO и mapper находятся в API. Реализованы Practitioner read/search/update, Bundle searchset, OperationOutcome, ETag, Last-Modified и If-Match. FHIR Resource не является Domain entity.

## Запуск и тесты

Нужны .NET SDK 10, PostgreSQL и RabbitMQ. Docker Compose в репозитории отсутствует. Migrations применяются командой `dotnet ef database update`; Worker запускается `dotnet run --project src/DoctorRegistry.Worker`. Тесты запускаются `dotnet test --no-restore`, integration tests используют Testcontainers.

## Вопросы для собеседования

**Почему один DbContext?** Practitioner и ProcessedMessage должны участвовать в одной PostgreSQL transaction.

**Как работает идемпотентность?** Уникальный MessageId и атомарный `ON CONFLICT DO NOTHING`; предварительный AnyAsync не используется как защита.

**Почему ACK после commit?** Иначе сообщение может быть удалено RabbitMQ до сохранения данных.

**Зачем ETag/If-Match?** Для защиты от незаметной перезаписи параллельного изменения.

**Почему retry ограничен?** Чтобы постоянные ошибки не создавали бесконечную requeue-loop; после трёх tiers сообщение попадает в DLQ.

**Почему нет Outbox?** Это сознательно отложенное решение; атомарность между PostgreSQL commit и RabbitMQ publish пока не гарантируется.

## Ограничения и предупреждения

- Docker Compose файла нет.
- Без PostgreSQL и RabbitMQ API/Worker не запустятся.
- После PostgreSQL commit и до RabbitMQ ACK возможна повторная доставка; MessageId делает её безопасной.
- MessageId не гарантирует порядок разных сообщений: источник не предоставляет version/timestamp ordering contract.
- Сборка выдаёт существующие NU1903 для транзитивного SSH.NET из Testcontainers.
