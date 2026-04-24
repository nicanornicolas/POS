# POS Backend

Production-oriented .NET 8 backend for POS transaction lifecycle management with strict financial invariants, idempotent webhook handling, and secure refund processing.

## Purpose

This service powers card payment transaction orchestration for POS terminals and focuses on:

- Financial correctness under retries, duplicates, and malformed payloads.
- Strict architecture boundaries (`Domain`, `Application`, `Infrastructure`, `Api`).
- Secure, layered webhook validation before settlement.
- Clear extension points for terminal notification and crypto conversion workflows.

## Architecture

The solution follows Clean Architecture with strict inward dependencies.

- `Domain`: Entities, value objects, enums, and business invariants.
- `Application`: CQRS/MediatR command handlers, contracts, and application exceptions.
- `Infrastructure`: EF Core + PostgreSQL persistence, Flutterwave gateway, integration handlers.
- `Api`: HTTP controllers, middleware, DI wiring, and request filters.
- `Tests`: Unit/integration-style tests for domain and high-risk command paths.

```text
Api/
Application/
Domain/
Infrastructure/
Tests/
pos.sln
```

## Runtime Stack

- .NET SDK: 8.x
- Database provider: PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`
- Local database container: `docker-compose.yml` (`postgres:16-alpine`, exposed on `localhost:5432`)

## API Contract

Base route prefix: `/api/v1`

### 1. Charge Initiation

- Method: `POST`
- Route: `/api/v1/transactions/charge`
- Body:

```json
{
  "amount": 1000,
  "currency": "KES",
  "paymentToken": "flw-t1nf-test",
  "terminalId": "TERM-003"
}
```

- Success response: `202 Accepted`

```json
{
  "transactionId": "guid",
  "status": "Pending",
  "message": "Charge initiated. Awaiting network confirmation."
}
```

### 2. Refund

- Method: `POST`
- Route: `/api/v1/transactions/{id}/refund`
- Success response: `200 OK`

```json
{
  "success": true,
  "message": "Refund processed successfully."
}
```

Possible failures include:

- `404 Not Found` (`TransactionNotFoundException`)
- `400 Bad Request` (`InvalidTransactionStateException`)
- `502 Bad Gateway` (`ProviderOperationFailedException`)

### 3. Flutterwave Webhook

- Method: `POST`
- Route: `/api/v1/webhooks/flutterwave`
- Required header: `verif-hash: <Flutterwave:WebhookHash>`
- Payload:

```json
{
  "data": {
    "id": 123456789
  }
}
```

- Success response: `200 OK`

## Security Model

Webhook processing uses layered validation:

1. API filter validates incoming `verif-hash` header.
2. Provider verification checks transaction status via Flutterwave API.
3. Local reconciliation confirms amount/currency and enforces idempotency.

Settlement is blocked if any validation layer fails.

## Exception-to-HTTP Mapping

Global exception middleware maps exceptions to status codes:

- `TransactionNotFoundException` -> `404 Not Found`
- `InvalidTransactionStateException` -> `400 Bad Request`
- `AmountMismatchException` -> `409 Conflict`
- `ProviderOperationFailedException` -> `502 Bad Gateway`
- Unhandled exception -> `500 Internal Server Error`

Error envelope:

```json
{
  "error": "<message>"
}
```

## Configuration

Required settings:

- `ConnectionStrings:DefaultConnection`
- `Flutterwave:SecretKey`
- `Flutterwave:WebhookHash`

Startup fail-fast behavior:

- The API throws during startup if `Flutterwave:SecretKey` or `Flutterwave:WebhookHash` is missing.

Example (`Api/appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ikonex_db;Username=postgres;Password=SuperSecurePosDB!2026;"
  },
  "Flutterwave": {
    "SecretKey": "FLWSECK_TEST-sandboxkey",
    "WebhookHash": "CORRECT_HASH"
  }
}
```

Do not store production secrets in source control.

## Local Development

### 1. Start PostgreSQL

```bash
docker compose up -d
```

### 2. Restore, Build, Test

```bash
dotnet restore
dotnet build pos.sln -m:1 -v minimal
dotnet test pos.sln -m:1 -v minimal
```

### 3. Run API

```bash
dotnet run --project Api/Api.csproj
```

## Regression Baseline (Current)

Repository regression check commands:

```bash
dotnet build pos.sln -m:1 -v minimal
dotnet test pos.sln -m:1 -v minimal
```

Current baseline status:

- Build: passed
- Tests: passed (`20/20`)
- Failing regressions: none observed

## License

This repository is proprietary and confidential software owned by Ikonex Systems.

See [LICENSE.txt](LICENSE.txt) for full legal terms and restrictions.
