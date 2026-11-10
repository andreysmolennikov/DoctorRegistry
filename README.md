# DoctorRegistry

DoctorRegistry — сервис на .NET 10 для управления записями `Practitioner`. Он предоставляет JSON CRUD, endpoint для асинхронного импорта и endpoints FHIR R5. Отдельный Worker получает версионированные сообщения импорта Practitioner из RabbitMQ и применяет их транзакционно в PostgreSQL.

## Architecture

Clean Architecture + Vertical Slice:

- `Domain`: сущность Practitioner и её инварианты.
- `Application`: сценарии использования, валидация, DTO, обработка импорта и контракты хранения данных.
- `Infrastructure`: EF Core/PostgreSQL, миграции, подключение к RabbitMQ, топология и publisher.
- `Api`: ASP.NET Core Minimal API.
- `Worker`: consumer на основе RabbitMQ BackgroundService.
- `UnitTests` и `IntegrationTests`: тесты xUnit, Testcontainers для PostgreSQL/RabbitMQ.

Решение использует один `DoctorRegistryDbContext`. Схемы PostgreSQL: `practitioner` и `messaging`.

## Technologies and prerequisites

.NET 10, ASP.NET Core Minimal API, PostgreSQL, EF Core, RabbitMQ.Client 7.2.1, FluentValidation, xUnit, Testcontainers и Docker Compose. Redis, MediatR, Outbox и generic repositories не реализованы.

Установите .NET 10 SDK и Docker. Стек Compose запускает PostgreSQL, RabbitMQ, API и Worker. Dockerfile для API и Worker находятся в `src/DoctorRegistry.Api/Dockerfile` и `src/DoctorRegistry.Worker/Dockerfile`.

## Configuration

```

По умолчанию Compose использует пользователя/базу данных PostgreSQL `doctorregistry`/`doctor_registry`, пароль PostgreSQL `doctorregistry-password` и пользователя/пароль RabbitMQ `doctorregistry`/`strong-password`. Перед запуском переопределите `POSTGRES_PASSWORD` и `RABBITMQ_PASSWORD`, если нужны другие значения.

## Docker Compose startup

Из корня репозитория выполните:

```bash
docker compose config
docker compose up --build -d
docker compose ps
docker compose logs -f api worker
```

API применяет миграции EF Core до начала прослушивания порта. Worker также идемпотентно применяет те же миграции перед запуском consumer RabbitMQ. Seed миграции создаёт Practitioner `11111111-1111-1111-1111-111111111111`; история миграций EF Core делает повторные запуски безопасными и не добавляет seed повторно.

Остановите стек командой:

```bash
docker compose down
```

Именованные volumes PostgreSQL и RabbitMQ сохраняются после `docker compose down`. Удаляйте их только при намеренном сбросе локальных данных.

## Database and startup

```bash
dotnet ef database update --project src/DoctorRegistry.Infrastructure --startup-project src/DoctorRegistry.Api
dotnet run --project src/DoctorRegistry.Api
dotnet run --project src/DoctorRegistry.Worker
```

Миграции создают `practitioner.practitioners` и `messaging.processed_messages`. Seed-данные включают Practitioner с ID `11111111-1111-1111-1111-111111111111`.

При использовании Docker Compose API доступен по адресу `http://localhost:8080`, документ OpenAPI — по адресу `http://localhost:8080/openapi/v1.json`, а Swagger UI — по адресу `http://localhost:8080/swagger/index.html`. RabbitMQ Management UI доступен по адресу `http://localhost:15672` с указанными выше учётными данными Compose. Worker запускается автоматически как сервис Compose `worker`.

Для локальной разработки API HTTP-профиль запуска использует `http://localhost:5154/openapi/v1.json` и `http://localhost:5154/swagger`; HTTPS-профиль — `https://localhost:7086/openapi/v1.json` и `https://localhost:7086/swagger`.

## JSON API examples

```bash
curl -X POST http://localhost:8080/api/practitioners -H 'Content-Type: application/json' -d '{"firstName":"Ada","lastName":"Lovelace","phone":"+375291234567","speciality":"Therapist","placeOfWork":"Central Clinic","email":"ada@example.test","isActive":true}'
curl -X POST http://localhost:8080/api/practitioners/import -H 'Content-Type: application/json' -d '{"practitionerId":"22222222-2222-2222-2222-222222222222","firstName":"Ada","lastName":"Lovelace","phone":"+375291234567","speciality":"Therapist","placeOfWork":"Central Clinic","email":"ada@example.test","isActive":true}'
curl 'http://localhost:8080/api/practitioners?name=Ada&isActive=true&page=1&pageSize=20'
curl http://localhost:8080/api/practitioners/{id}
curl -X PUT http://localhost:8080/api/practitioners/{id} -H 'Content-Type: application/json' -d '{"firstName":"Ada","lastName":"Lovelace","phone":"+375291234567","speciality":"Therapist","placeOfWork":"Central Clinic","email":"ada@example.test","isActive":true,"version":"{version-guid}"}'
curl -X DELETE 'http://localhost:8080/api/practitioners/{id}?version={version-guid}'
```

FHIR examples:

```bash
curl -H 'Accept: application/fhir+json' http://localhost:8080/fhir/Practitioner/{id}
curl -H 'Accept: application/fhir+json' 'http://localhost:8080/fhir/Practitioner?name=Ada&_count=20&page=1'
```

Ответы FHIR используют ресурсы Practitioner, Bundle и OperationOutcome. Обновление FHIR использует `If-Match`, ETag и Last-Modified.

### Asynchronous import

`POST /api/practitioners/import` принимает существующий JSON-контракт `PractitionerImportMessageV1`. `practitionerId` и поля Practitioner обязательны. `messageId` необязателен: если он не указан, API генерирует новый GUID. `schemaVersion` также необязателен: если он не указан, используется текущая версия (`1`). При успешной публикации возвращается `202 Accepted` с сгенерированным или переданным `messageId`.

Example request:

```json
{
  "practitionerId": "22222222-2222-2222-2222-222222222222",
  "firstName": "Ada",
  "lastName": "Lovelace",
  "phone": "+375291234567",
  "speciality": "Therapist",
  "placeOfWork": "Central Clinic",
  "email": "ada@example.test",
  "isActive": true
}
```

Successful response (`202 Accepted`):

```json
{
  "messageId": "generated-or-supplied-message-id"
}
```

Некорректные payload возвращают существующую JSON-ошибку валидации (`400`). Если публикация в RabbitMQ завершается ошибкой, существующий обработчик исключений возвращает общий ответ с ошибкой (`500`); сообщение не считается принятым.

Поток работает так: API валидирует запрос и публикует его через `IPractitionerImportPublisher` в RabbitMQ; Worker получает сообщение, валидирует и обрабатывает его транзакционно; PostgreSQL сохраняет Practitioner и обработанный `MessageId`. Ответ API подтверждает публикацию, а не завершение записи в базу данных.

## RabbitMQ

Worker получает сообщения из `doctor-registry.practitioner-import` с ручным ACK и отдельным DI scope для каждого сообщения. Для durable retry queues используются TTL 30 секунд, 5 минут и 30 минут; постоянные или исчерпавшие попытки ошибки отправляются в DLQ. ACK выполняется после фиксации транзакции базы данных или подтверждения публикации замещающего сообщения. В Compose Worker запускается автоматически; для прямого локального запуска после настройки конфигурации выше используйте `dotnet run --project src/DoctorRegistry.Worker`.

## Tests

Запустите `dotnet format --no-restore`, `dotnet build --no-restore` и `dotnet test --no-restore`. Интеграционные тесты используют Testcontainers для PostgreSQL и RabbitMQ.
