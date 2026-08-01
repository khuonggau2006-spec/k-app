# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

TaskMgmt — task/work-location management system. Backend: .NET 10 (Clean Architecture). Mobile: Flutter (feature-first). Infra: PostgreSQL, Redis, Hangfire, MinIO (S3-compatible), all via Docker.

Docs live in `docs/` (Vietnamese): `KE-HOACH-TRIEN-KHAI.md` is the phased delivery plan (G0–G5) that explains *why* features exist (e.g. TaskHistory = audit trail requirement, Notifications = FR-7). `CONTRIBUTING.md` has branching/commit conventions. Repo prose (docs, comments, commit examples) is largely Vietnamese; code identifiers are English.

## Commands

### Backend (`backend/`)

```bash
docker compose up -d                              # Postgres, Redis, pgAdmin, RedisInsight, MinIO (repo root)

cd backend
dotnet restore TaskMgmt.slnx
dotnet build TaskMgmt.slnx
dotnet test TaskMgmt.slnx                         # all test projects
dotnet test tests/TaskMgmt.Application.UnitTests   # single project
dotnet test --filter "FullyQualifiedName~CreateWorkTaskCommandHandlerTests"  # single test/class

cd src/TaskMgmt.API && dotnet run                 # API at /swagger, health at /health
```

Note: the solution file is `TaskMgmt.slnx` (new XML format), not `.sln` — the CI workflow (`.github/workflows/backend-ci.yml`) currently references `TaskMgmt.sln`, which does not exist in this repo; use `TaskMgmt.slnx` when running commands locally.

EF Core migrations live in `TaskMgmt.Infrastructure/Persistence/Migrations`. Add new ones from `backend/src/TaskMgmt.Infrastructure`:
```bash
dotnet ef migrations add <Name> --startup-project ../TaskMgmt.API
```

Three test projects: `TaskMgmt.Domain.UnitTests`, `TaskMgmt.Application.UnitTests` (bulk of coverage — handler/validator/behavior tests using in-memory `TestDbContextFactory` and `Fake*` service doubles under `Common/`), `TaskMgmt.API.IntegrationTests` (uses `TaskMgmtApiFactory`, a `WebApplicationFactory`-style harness).

### Mobile (`mobile/taskmgmt_app/`)

```bash
cd mobile/taskmgmt_app
flutter pub get
flutter analyze
flutter test                                       # all tests
flutter test test/work_task_detail_test.dart        # single file
flutter run
```

Code generation (json_serializable/freezed) is referenced in `pubspec.yaml` dev_dependencies but no `build.yaml`/generated files exist yet — run `dart run build_runner build --delete-conflicting-outputs` if/when `.g.dart`/`.freezed.dart` files are introduced.

## Backend architecture

Clean Architecture, strict dependency direction `API → Application → Domain`, with `Infrastructure → Application/Domain` (implements Application's interfaces):

- **Domain** — entities (`WorkTask`, `Location`, `Comment`, `Attachment`, `TaskAssignee`, `TaskHistory`, `Notification`, `DeviceToken`, `RefreshToken`, `User`), enums, and domain events (`Events/*Events.cs`). Entities derive from `AuditableEntity`/`BaseEntity` and expose `AddDomainEvent`/`ClearDomainEvents`. No dependencies on other layers.
- **Application** — CQRS via MediatR. Each use case lives at `Features/<Module>/{Commands,Queries}/<UseCase>/` as a `Command`/`Query` + `Handler` + FluentValidation `Validator` (validation is never done manually in handlers — `ValidationBehavior` runs validators as a MediatR pipeline behavior). Cross-cutting interfaces (`ICacheService`, `IApplicationDbContext`, `IRealtimeNotifier`, `IPushNotificationService`, `IBackgroundJobScheduler`, `IFileStorageService`, `ICurrentUserService`, ...) are defined here and implemented in Infrastructure/API.
- **Infrastructure** — EF Core (`AppDbContext`, `Persistence/Configurations/*`, `Persistence/Migrations/*`), Redis cache (`RedisCacheService`, falls back to `NullCacheService` if Redis isn't configured), Hangfire jobs (`BackgroundJobs/*`, falls back to `NoOpBackgroundJobScheduler`), Firebase Admin SDK push (`Notifications/*`), S3/MinIO file storage, JWT/password hashing.
- **API** — controllers, JWT bearer auth, `SystemRole`-based authorization policies (`RequireManager`/`RequireAdmin`, custom `SystemRoleAuthorizationHandler`), SignalR hub (`NotificationHub` at `/hubs/notifications`), Hangfire dashboard at `/hangfire` (protected by `HangfireDashboardAuthorizationFilter`).

**Domain event fan-out is the core extension pattern.** Handlers call `entity.AddDomainEvent(...)` (e.g. `WorkTaskStatusChangedEvent`, `WorkTaskFieldChangedEvent`) *before* mutating the entity, so old/new values are captured correctly. `DispatchDomainEventsInterceptor` (an EF Core `SaveChangesInterceptor`) publishes queued domain events via MediatR immediately before `SaveChanges` writes to the DB — event handlers only call `context.Add(...)` (never `SaveChanges` themselves), so the business change and its side effects (history row, notification, realtime push) land in the *same* transaction. Three independent handler families subscribe to the same domain events, each under its own `Features/<Concern>/EventHandlers/`:
  - `Features/TaskHistories/EventHandlers/*` — writes immutable `TaskHistory` audit rows (verified never editable/deletable via API, even by Admin — see `TaskHistoryImmutabilityTests`).
  - `Features/Notifications/EventHandlers/*` — creates `Notification` rows and triggers push (via Hangfire `SendPushNotificationJob`).
  - `Features/Realtime/EventHandlers/*` — pushes live updates to connected clients through `IRealtimeNotifier` (SignalR).

  When adding a new mutation that should be audited/notified/broadcast, raise a domain event and add handlers in the relevant families rather than doing it inline in the command handler.

- **Permissions** are two-tiered: `SystemRole` (Admin/Manager/Member) gates API-level policies; `TaskAssigneeRole` (Owner/Assignee/Reviewer/Watcher) gates per-task actions and is checked in `Features/TaskAssignees/Common/TaskAssigneeAuthorization.cs`.
- **Caching** is cache-aside via `ICacheService`, keyed through `Common/Caching/CacheKeys.cs`. Mutation handlers explicitly invalidate (`cache.RemoveAsync(...)`, `cache.RemoveByPrefixAsync(...)`) — there's no automatic invalidation, so new cached queries need matching removal calls in every handler that mutates that data.
- **WorkTask hierarchy**: subtasks via self-referencing `ParentTaskId`, limited to 3 levels (`Features/WorkTasks/Common/WorkTaskHierarchy.cs`).
- Recurring jobs (`SendDueSoonReminderJob`, `SendOverdueReminderJob`, `CleanupExpiredTokensJob`) are registered idempotently in `Program.cs` via `RecurringJob.AddOrUpdate` on every startup; disable locally with config `Hangfire:Disabled`.
- SignalR/Hangfire/API auth all share one JWT bearer scheme; because browsers/WebSocket clients can't set the `Authorization` header, `/hubs` and `/hangfire` (only those two paths) also accept the token via `?access_token=` query string (see `Program.cs`).

## Mobile architecture

Feature-first under `lib/features/<feature>/`, each with its own `data/` (datasource + model + repository impl), `domain/` (entity + repository interface), `presentation/` (Riverpod providers + screens/widgets) — mirroring the backend's one-module-per-concern layout. Shared infra goes in `lib/core/` (`network/` — Dio client + interceptors + `AuthEventBus`, `routing/`, `storage/` — secure token storage, `push/`), cross-feature UI/state helpers would go in `lib/shared/` (per `CONTRIBUTING.md`; not yet used).

- **DI**: `get_it` service locator, all registrations centralized in `lib/core/di/injection.dart` (`setupLocator()`), called once at startup. New repositories/datasources are wired here, not with a DI framework/annotations.
- **State**: Riverpod (`flutter_riverpod`). `AuthEventBus` + `_AuthRefreshNotifier` in `app_router.dart` drive `go_router`'s redirect logic — auth state changes trigger router re-evaluation, not manual navigation calls.
- **Networking**: single shared `Dio` instance (`core/network/dio_client.dart`) with an `auth_interceptor.dart` that attaches tokens and handles refresh; `ApiException` normalizes errors for the UI layer.
- **Push**: `core/push/` wraps `firebase_messaging` + `flutter_local_notifications`; device token registration/unregistration is tied to login/logout via the auth event bus (see comment in `app.dart`).
- Routes are named/typed via static `path`/`name` constants on each screen widget (e.g. `LoginScreen.path`), referenced from `app_router.dart` rather than hardcoded route strings.

## Conventions (from `CONTRIBUTING.md`)

- Branches: `main` (prod, PR-only) ← `develop` (staging) ← `feature/*`, `bugfix/*`, `hotfix/*`.
- Conventional Commits: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.
- C#: follow `backend/.editorconfig` (4-space indent, CRLF, file-scoped namespaces, `var` preferred); namespaces mirror folder structure; every use case is a Command/Query + Handler under `Application/Features/<Module>`; validate via FluentValidation only, never manually in handlers.
- Dart: default `flutter_lints`; every feature is self-contained (`data`/`domain`/`presentation`); shared technical infra → `lib/core/`, shared UI/models → `lib/shared/`.
