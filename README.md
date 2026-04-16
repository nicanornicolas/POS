# POS Backend (FinTech MVP)

Production-oriented, cleanly layered .NET 8 backend for card payment lifecycle management with strict financial invariants, idempotent webhook handling, and secure refund processing.

## 1. Purpose

This backend powers POS transaction lifecycle operations for a cashier-facing terminal flow and is designed for:

- Correctness under edge cases (retries, duplicate callbacks, malformed payloads).
- Strong separation of concerns (Domain, Application, Infrastructure, API).
- Fail-fast behavior for critical configuration.
- Extensibility toward future payment rails and Web3 settlement engines.

## 2. Current Scope

Implemented in this MVP:

- Domain model for monetary values, transaction state machine, and ledger entries.
- Application command handlers for:
  - Refund processing
  - Webhook settlement processing
- Infrastructure adapters:
  - EF Core persistence context and mappings
  - Typed Flutterwave client with retry resilience
- API surface:
  - Thin controller endpoints
  - Layer 1 webhook signature filter
  - Global exception handling middleware
- Automated tests for domain invariants and high-risk paths.

Out of current scope (planned next):

- Charge initiation endpoint (`POST /api/v1/transactions/charge`)
- Device authentication and JWT issuance flow
- Push confirmation channel back to POS app (SignalR/FCM)

## 3. Architecture

The solution follows Clean Architecture with strict inward dependencies.

- Domain: Entities, value objects, enums, business invariants.
- Application: Use-case orchestration via MediatR, contracts, explicit exceptions.
- Infrastructure: EF Core implementation, provider gateway implementation.
- Api: HTTP concerns only, middleware, DI, routing.
- Tests: Deterministic unit tests for invariants and risk paths.

### 3.1 Project Structure

```text
Api/
Application/
Domain/
Infrastructure/
Tests/
pos.sln
```

## 4. Security Model

Webhook processing uses layered defenses:

1. Layer 1 (API filter): Verify incoming `verif-hash` header matches configured secret.
2. Layer 2 (provider verification): Verify webhook transaction against provider API.
3. Layer 3 (local reconciliation): Confirm amount/currency match local transaction and enforce idempotency.

If any layer fails, settlement is blocked.

## 5. Financial Invariants

Core invariants enforced by design:

- Money cannot be negative.
- Money arithmetic requires matching currencies.
- Transaction state transitions are guarded:
  - `Pending -> Settled -> Refunded`
  - Invalid transitions throw explicit exceptions.
- Ledger entries are double-entry style with required debit/credit semantics.
- Duplicate webhook processing does not duplicate financial side effects.
- Refund of non-settled transactions is rejected.

## 6. API Contract (Android Handoff)

### 6.1 Initiate Refund

- Method: `POST`
- Route: `/api/v1/transactions/{id}/refund`
- Trigger: Cashier selects a transaction and presses Void/Refund.

Headers:

- `Content-Type: application/json`
- `Authorization: Bearer <Terminal_JWT>` (planned for device auth phase)

Success response (`200 OK`):

```json
{
  "success": true,
  "message": "Refund processed successfully."
}
```

Validation/state failure example (`400 Bad Request`):

```json
{
  "error": "Only settled transactions can be refunded."
}
```

Other possible responses:

- `404 Not Found` if transaction does not exist.
- `502 Bad Gateway` if provider operation fails.

### 6.2 Flutterwave Webhook

- Method: `POST`
- Route: `/api/v1/webhooks/flutterwave`
- Caller: Flutterwave (not Android app)

Required header:

- `verif-hash: <YourConfiguredWebhookHash>`

Payload shape:

```json
{
  "data": {
    "id": 123456789
  }
}
```

Behavior:

- Validates hash (Layer 1)
- Verifies with provider (Layer 2)
- Reconciles amount/currency + idempotency (Layer 3)
- Settles local transaction and writes ledger entries
- Returns `200 OK` after successful processing path

## 7. Exception-to-HTTP Mapping

Global exception middleware maps application exceptions to status codes:

- `TransactionNotFoundException` -> `404 Not Found`
- `InvalidTransactionStateException` -> `400 Bad Request`
- `AmountMismatchException` -> `409 Conflict`
- `ProviderOperationFailedException` -> `502 Bad Gateway`
- Any unhandled exception -> `500 Internal Server Error`

Error response envelope:

```json
{
  "error": "<message>"
}
```

## 8. Configuration

### 8.1 Required Settings

Set these before running the API:

- `ConnectionStrings:DefaultConnection`
- `Flutterwave:SecretKey`
- `Flutterwave:WebhookHash`

### 8.2 Fail-Fast Startup Guard

The API validates critical Flutterwave settings before startup. If either key is missing, the service throws during boot and does not start.

This prevents runtime payment failures during checkout.

### 8.3 Example appsettings.Development.json snippet

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=PosDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Flutterwave": {
    "SecretKey": "FLWSECK_TEST_xxx",
    "WebhookHash": "your_webhook_hash"
  }
}
```

Do not commit production secrets. Use user secrets, environment variables, or a secret manager.

## 9. Local Development

Prerequisites:

- .NET SDK 8.0+
- SQL Server instance (for runtime API persistence)

Install/restore and build:

```bash
dotnet restore
dotnet build pos.sln
```

Run tests:

```bash
dotnet test pos.sln
```

Run API:

```bash
dotnet run --project Api/Api.csproj
```

## 10. Testing Strategy

### 10.1 Domain Invariant Tests

- Money validation and arithmetic constraints
- Transaction transition guard clauses and idempotency
- Ledger entry creation constraints

### 10.2 High-Risk Path Tests

Using EF Core InMemory + mocked provider client:

- Refund on pending transaction is blocked.
- Re-refund path is idempotent and avoids provider re-call.
- Webhook amount mismatch throws and does not settle.
- Duplicate webhook returns success without state mutation.
- Webhook filter rejects missing/invalid signature headers.

## 11. Operational Notes

- Typed `HttpClient` for provider integration includes exponential backoff retry policy.
- Ensure webhook source can reach deployed backend over HTTPS.
- Monitor logs for exception middleware outputs and provider retry behavior.

## 12. Android (Kotlin / Sunmi V2 Pro) Integration Prep

Immediate preparation checklist for Android team:

1. Hardware readiness:
- Charge Sunmi V2 Pro.
- Confirm stable Kenyan LTE connectivity (Safaricom/Airtel).
- Enable developer options.

2. SDK imports:
- SunmiPaySDK (secure EMV/AIDL integration)
- SunmiPrinterSDK (thermal receipt printing)
- Flutterwave Android POS SDK (DUKPT encrypted card data flow)

3. UI state machine scaffolding (Jetpack Compose ViewModel):
- `Idle -> AwaitingTap -> Processing -> Approved -> Printing`

4. Backend interaction assumptions:
- POS app calls refund endpoint as specified.
- POS app does not call webhook endpoint directly.
- POS app can remain in "Waiting for Confirmation" until backend settlement confirmation strategy is added.

## 13. Suggested Next Backend Increment

- Add `POST /api/v1/transactions/charge` via the same CQRS pattern.
- Add auth middleware and issue device-bound JWT tokens.
- Add push notification channel (SignalR/FCM) for terminal confirmation and printer trigger.

## 14. Repository Health Snapshot

Current status at this phase:

- Clean architecture layering in place.
- Deterministic unit tests green.
- High-risk financial and webhook paths covered.
- API contract ready for Android handoff.
