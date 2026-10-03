# Architecture Guidelines

This solution is the architecture template for larger systems built with the same stack and design decisions. Treat the patterns in this repository as normative. New features must preserve the same layering, dependency direction, naming, validation, authorization, persistence, logging, and testing style.

This document is written for both human developers and coding agents. If a change conflicts with these rules, change the design before changing the code.

## Core Architecture

The solution is a narrow ASP.NET Core DDD template using:

- ASP.NET Core API controllers
- Auth0 JWT authentication
- Application-owned authorization from database-backed users, roles, and branch scope
- Domain-driven layering
- EF Core with PostgreSQL via Npgsql
- UUID v7 identifiers
- FluentValidation for application request validation
- RFC 7807-style ProblemDetails responses
- Serilog structured JSON logging
- Immediate domain events dispatched after successful persistence
- Testcontainers-backed integration and persistence tests

The project dependency direction is fixed:

```text
LibraryTemplate.Domain
  ^
  |
LibraryTemplate.Application
  ^             ^
  |             |
LibraryTemplate.Persistence
LibraryTemplate.Infrastructure
  ^             ^
  |             |
LibraryTemplate.Api
```

Allowed project references:

- `LibraryTemplate.Domain` has no project references.
- `LibraryTemplate.Application` references `LibraryTemplate.Domain`.
- `LibraryTemplate.Persistence` references `LibraryTemplate.Application` and `LibraryTemplate.Domain`.
- `LibraryTemplate.Infrastructure` references `LibraryTemplate.Application`.
- `LibraryTemplate.Api` references `LibraryTemplate.Application`, `LibraryTemplate.Infrastructure`, and `LibraryTemplate.Persistence`.
- Test projects may reference the projects they test and any needed lower layers.

Violates the rules:

- Referencing `Persistence`, `Infrastructure`, or `Api` from `Domain`.
- Referencing `Persistence`, `Infrastructure`, or `Api` from `Application`.
- Putting EF Core, ASP.NET Core MVC, Auth0, HTTP, or database implementation types into `Domain` or `Application`.
- Adding a direct reference only to make a convenient call instead of defining a port in the correct layer.

## Solution Structure

Use the existing project folders as the placement model.

```text
LibraryTemplate.Api/
  BackgroundJobs/
  Contracts/
  Controllers/
  Errors/
  Options/
  Security/

LibraryTemplate.Application/
  BackgroundJobs/
  Dependencies/
    ExternalServices/
    Infrastructure/
    Persistence/
  Events/
  Services/

LibraryTemplate.Domain/
  Aggregates/
  Dependencies/
    Persistence/
  Events/
  Services/

LibraryTemplate.Infrastructure/
  Email/
  Options/
  Time/

LibraryTemplate.Persistence/
  Migrations/
  Options/
  Repositories/
  Rows/

*.Tests/
```

Rules:

- Place new code by responsibility, not by convenience.
- Keep feature-facing application code under `Application/Services/<FeatureName>/`.
- Keep API transport contracts under `Api/Contracts/`.
- Keep persistence-only join/security rows under `Persistence/Rows/`.
- Keep repository interfaces in `Domain/Dependencies/Persistence` when they return aggregates or enforce aggregate persistence contracts.
- Keep query/read-model repository interfaces in `Application/Dependencies/Persistence` when they return custom projections for use cases.
- Keep technical external-service abstractions in `Application/Dependencies/ExternalServices`.
- Keep infrastructure abstractions such as time in `Application/Dependencies/Infrastructure`.
- Keep implementation registrations in each layer's `*Services` extension class.

Violates the rules:

- Creating a generic `Common`, `Shared`, or `Helpers` project for mixed concerns.
- Putting use-case orchestration into controllers.
- Putting SQL or EF queries into application services.
- Putting domain invariants into API request validators.
- Returning EF entities or domain aggregates directly from API endpoints.

## Domain Layer Rules

The Domain layer contains business concepts and rules. It must remain persistence-ignorant and framework-ignorant.

Use Domain for:

- Aggregates and entities under `Domain/Aggregates/<AggregateArea>/`.
- Strongly typed IDs, colocated with the aggregate or entity they identify.
- Value objects such as `Isbn` and `LoanNumber`.
- Domain exceptions with stable rule codes.
- Domain events and aggregate event collection.
- Domain services for business rules that span multiple aggregates.
- Repository ports needed by domain services.
- Domain enums that are part of the business language.

Aggregate rules:

- Aggregate roots inherit from `AggregateRoot<TId>` when they raise domain events.
- Constructors used by EF must be private and minimal.
- Public mutation must happen through named methods such as `Block`, `MarkDeleted`, or `Start`.
- Setters should be private.
- Collection fields should be private mutable lists exposed as `IReadOnlyCollection<T>`.
- Creation should use static factories when invariants must be enforced.
- Domain methods must throw `DomainException` for business rule failures.
- `DomainException.Code` must be stable and namespaced by concept, for example `Loan.BookBlocked`.

Strongly typed ID rules:

- IDs are readonly record structs with a single `Guid Value`.
- New IDs are generated with `Guid.CreateVersion7()`.
- IDs override `ToString()` by returning `Value.ToString()`.
- Do not pass raw `Guid` values deep into domain behavior when a strongly typed ID exists.

Value object rules:

- Use readonly record structs for small immutable values.
- Hide constructors when validation or normalization is required.
- Provide `Create` for validated construction.
- Provide `IsValid` only when callers need pre-validation.
- Normalize input in the value object, not in controllers or EF mappings.
- Override `ToString()` to return the persisted/display value.

Domain service rules:

- Use a domain service only when business logic spans aggregates or cannot naturally belong to one aggregate.
- Domain services may depend on domain repository ports.
- Domain services must not depend on EF Core, logging, HTTP, options, configuration, or infrastructure implementations.

Domain event rules:

- Domain events implement `IDomainEvent`.
- Aggregates raise events through `AggregateRoot.Raise`.
- Event payloads use domain types, strongly typed IDs, and simple values.
- Domain events represent something that already happened in domain language.
- Do not send email, publish messages, or perform I/O from aggregate methods.

Violates the rules:

- A controller checking whether a child can borrow an adult-only book.
- A repository deciding whether a blocked book can be borrowed.
- An aggregate injecting `ILogger`, `DbContext`, `IConfiguration`, or an email sender.
- A domain event handler mutating an aggregate before the original transaction is saved.
- Adding business-rule exceptions without stable `Code` values.

## Application Layer Rules

The Application layer orchestrates use cases. It validates input, authorizes the current app user, loads required data through ports, invokes domain behavior, commits once, and returns application DTOs.

Use Application for:

- App service interfaces and implementations under `Application/Services/<FeatureName>/`.
- Application request and response contracts.
- FluentValidation validators for application requests.
- Application-level exceptions: `NotFoundException`, `ForbiddenException`, `UnauthenticatedException`, and `ConflictException`.
- Current-user abstraction through `ICurrentUserProvider`.
- Authorization orchestration based on app-owned users, roles, and branch scope.
- Background job use-case logic.
- Domain event handlers.
- Ports for persistence projections, external services, clock/time, and other technical dependencies.
- Dependency injection registration in `ApplicationServices`.

Application service flow is fixed:

1. Convert raw IDs to strongly typed IDs at the edge of the app service method.
2. Validate the application request using FluentValidation.
3. Authorize using `ApplicationServiceBase` helpers or equivalent app-owned authorization logic.
4. Run sanity checks and load required aggregates/projections through repository ports.
5. Invoke domain methods or domain services.
6. Add new aggregates through repository ports when needed.
7. Commit exactly once through `IUnitOfWork.SaveChangesAsync()`.
8. Perform after-save logging or response shaping.
9. Return an application response DTO or projection-derived response.

Application authorization rules:

- Auth0 proves identity only.
- `ICurrentUserProvider.ExternalUserId` supplies the authenticated external subject.
- The application database maps external subjects to app users.
- Roles come from `User.Roles` / role assignments in the database.
- Branch scope comes from app-owned user-branch assignments.
- Authorization decisions belong in Application, not API attributes beyond `[Authorize]`.
- Use `ForbiddenException` when an authenticated user lacks app permission.
- Use `UnauthenticatedException` only when the authenticated request cannot provide a subject.

Request and response rules:

- Application request names end with `AppRequest`.
- Application response names end with `AppResponse`.
- Contracts for a feature live beside that feature's app service in `<FeatureName>Contracts.cs`.
- Validators live in the same contracts file unless the file becomes too large.
- Validators check input shape and use-case request constraints.
- Domain invariants must still be enforced in Domain even if validators pre-check the same condition.

Read-model rules:

- Use application projection repositories for custom query shapes.
- Projection records live beside the application read repository interface.
- Projection repositories return use-case-shaped data, not EF entities.
- Application responses may expose `FromProjection` methods for explicit projection mapping.

Event handler rules:

- Domain event handlers live in `Application/Events`.
- Handlers implement `IDomainEventHandler<TEvent>`.
- Handlers may call application ports such as repositories or external services.
- Handlers are dispatched by `DomainEventDispatcher`.
- Handlers must be registered in `ApplicationServices`.

Background job rules:

- The repeatable business logic for a background process lives in `Application/BackgroundJobs`.
- The hosted worker/timer lives in `Api/BackgroundJobs`.
- The application job depends on ports, not API or infrastructure implementations.
- The API worker creates scopes because hosted services are singleton infrastructure.

Violates the rules:

- Calling `DbContext.SaveChangesAsync()` directly from an app service.
- Skipping `IUnitOfWork` for writes.
- Using Auth0 roles, permissions, or custom claims as the source of app authorization.
- Validating only in controllers and not in application requests.
- Sending emails directly from controllers or aggregates.
- Returning domain aggregates from app services to controllers.

## API Layer Rules

The API layer is the transport boundary and composition host. Keep controllers thin.

Use API for:

- ASP.NET Core host configuration in `Program.cs`.
- Controllers and route declarations.
- `[ApiController]` and `[Authorize]` attributes.
- API request and response DTOs.
- Explicit mapping between API DTOs and Application DTOs.
- Auth0 JWT bearer configuration.
- `HttpCurrentUserProvider`.
- ProblemDetails exception mapping.
- Hosted background workers.
- API-specific options.
- Health checks and OpenAPI mapping.

Controller rules:

- Controllers depend on application service interfaces.
- Controllers map `*ApiRequest` to `*AppRequest` explicitly.
- Controllers map `*AppResponse` to `*ApiResponse` explicitly.
- Controllers select HTTP status codes such as `Created`, `Ok`, `Unauthorized`, `Forbidden`, or `Conflict`.
- Controllers do not query repositories directly.
- Controllers do not enforce business rules.
- Controllers do not call domain methods directly.
- Controllers do not call `IUnitOfWork`.

API contract rules:

- API request names end with `ApiRequest`.
- API response names end with `ApiResponse`.
- API responses expose explicit `FromAppResponse` mapping methods.
- API contracts are transport-facing and may differ from app contracts when the wire shape requires it.
- Do not reuse application DTOs as controller action parameters or response types.
- Do not expose persistence rows, EF entities, or domain aggregates as wire contracts.

Auth0 rules:

- Configure JWT bearer authentication in the API host.
- Use Auth0 `Domain` as `Authority`.
- Use Auth0 `Audience` as `Audience`.
- Keep `MapInboundClaims = false`.
- Extract `sub` first, then fallback to `ClaimTypes.NameIdentifier`.
- Do not put business authorization in Auth0 configuration.

Error handling rules:

- Centralize exception-to-ProblemDetails mapping in `ApiExceptionHandler`.
- Validation failures return `400`.
- Not found failures return `404`.
- Forbidden failures return `403`.
- Unauthenticated failures return `401`.
- Conflicts and domain rule failures return `409`.
- Domain rule failures include the domain exception `code` extension.
- Unexpected failures return `500` and are logged as errors.

Violates the rules:

- Adding business checks in a controller because it is "just one if statement."
- Injecting repositories into controllers for normal use cases.
- Returning anonymous objects instead of API response contracts.
- Returning raw exception messages for unexpected errors.
- Using `[Authorize(Roles = "...")]` as the source of app authorization.

## Persistence Layer Rules

The Persistence layer implements database access using EF Core and PostgreSQL. It adapts domain/application ports to EF.

Use Persistence for:

- `LibraryDbContext`.
- EF mappings.
- EF value converters for strongly typed IDs and value objects.
- Repository implementations.
- `UnitOfWork`.
- Persistence-only row types.
- EF migrations and model snapshots.
- Database options and design-time DbContext factory.
- Persistence dependency injection registration.

EF mapping rules:

- Use PostgreSQL through `UseNpgsql`.
- Configure all mappings in `LibraryDbContext.OnModelCreating`.
- Split mappings by domain area with private static methods such as `ConfigureBooks`.
- Use explicit table names.
- Existing table names are snake_case except intentional legacy column names such as `Auth0Subject`, `AppUserId`, and `RoleName`.
- Strongly typed IDs must use value converters to and from `Guid`.
- IDs are generated by the domain and configured with `ValueGeneratedNever()`.
- Value objects must use explicit conversions, for example `Isbn` and `LoanNumber`.
- Enums are stored as strings with explicit max lengths.
- Required string properties must have max lengths.
- Add unique indexes for natural uniqueness rules that must be database-enforced.
- Ignore `DomainEvents` on aggregate mappings.
- For aggregate child collections backed by private fields, configure property access mode.
- Use `DeleteBehavior.Restrict` where cascading would violate business ownership.

Repository rules:

- Repository implementations live in `Persistence/Repositories`.
- Write repositories return aggregates when mutation is expected.
- Read-only existence/scope checks should use `AsNoTracking()`.
- Projection repositories should use `AsNoTracking()` and project directly into projection records.
- Repositories should not contain business rule decisions.
- Repositories should normalize through domain APIs when lookup semantics require it.
- Repositories should not call `SaveChangesAsync()` except through `UnitOfWork`.

Persistence row rules:

- Use persistence-only row types for join tables, role assignments, and database structures that are not domain aggregates.
- Persistence row types live in `Persistence/Rows`.
- Persistence rows may reference domain IDs and enums.
- Do not move persistence-only rows into Domain to satisfy EF.

Unit of work and domain event rules:

- All application writes commit through `IUnitOfWork.SaveChangesAsync()`.
- `UnitOfWork` gathers domain events from tracked aggregates before saving.
- `UnitOfWork` calls EF `SaveChangesAsync()`.
- Domain events dispatch only after a successful save.
- Domain events are cleared after dispatch.
- Do not dispatch domain events before persistence succeeds.

Migration rules:

- Use EF Core migrations under `Persistence/Migrations`.
- Migrations must reflect `LibraryDbContext` mappings.
- Do not hand-edit the model snapshot except as part of a deliberate migration correction.
- Keep database-specific decisions PostgreSQL-compatible.
- Do not introduce a second database technology in this template architecture.

Violates the rules:

- Using Dapper, raw ADO.NET, or another ORM for normal repository work without an explicit architecture change.
- Storing enums as integers.
- Letting the database generate aggregate IDs.
- Saving changes inside individual repository methods.
- Returning `IQueryable` from repository ports.
- Adding EF annotations to Domain classes.
- Moving app authorization data out of the app database and into Auth0 claims.

## Infrastructure Layer Rules

The Infrastructure layer implements technical adapters behind Application ports.

Use Infrastructure for:

- Clock implementation.
- Email sender implementation.
- Options for infrastructure adapters.
- Dependency injection registration in `InfrastructureServices`.
- Future external service clients, file storage adapters, message senders, and similar technical integrations.

Rules:

- Infrastructure depends on Application abstractions.
- Infrastructure implementations should be replaceable without changing Application or Domain.
- Bind adapter options from named sections.
- Validate options with data annotations and `ValidateOnStart()`.
- Use structured logging for adapter behavior.
- Keep adapter contracts in Application, not Infrastructure.

Violates the rules:

- Defining `IEmailSender` in Infrastructure.
- Calling Infrastructure implementations directly from controllers.
- Letting Infrastructure reference Persistence unless a specific adapter truly needs it and the dependency is reviewed.
- Putting business policy inside an external-service adapter.

## Configuration, Options, and Host Rules

Configuration is strongly typed and validated at startup.

Rules:

- Each option class defines `SectionName`.
- Required values use data annotations.
- Options are bound with `BindConfiguration`.
- Options call `ValidateDataAnnotations()`.
- Options that are required for startup call `ValidateOnStart()`.
- Database connection string lives under `Database:ConnectionString`.
- Auth0 settings live under `Auth0:Domain` and `Auth0:Audience`.
- Email settings live under `Email`.
- Background job settings live under `BackgroundJobs`.
- Keep defaults safe for local development.

Host rules:

- `Program.cs` is the composition root.
- Register Application, Infrastructure, and Persistence through their extension methods.
- Keep Serilog bootstrap logging before host creation.
- Use Serilog request logging.
- Map OpenAPI only in development.
- Map `/health` with ASP.NET Core health checks.

Violates the rules:

- Reading raw configuration values throughout application services.
- Binding options manually in controllers.
- Starting background business workflows directly in `Program.cs`.
- Logging with unstructured string interpolation instead of structured templates.

## Logging Rules

Serilog is the logging decision for this template.

Rules:

- Use Serilog as the host logger.
- Write console logs as JSON.
- Use structured message templates with named properties.
- Log expected handled exceptions as information in the exception handler.
- Log unexpected exceptions as errors.
- Log meaningful use-case completion events after successful saves.
- Do not log secrets, tokens, connection strings, or sensitive personal data beyond existing safe identifiers needed for diagnostics.

Violates the rules:

- Replacing Serilog with another logging stack without an explicit architecture decision.
- Writing `Console.WriteLine` for application diagnostics.
- Logging JWTs or Auth0 tokens.
- Logging by concatenating values into message strings.

## Testing Rules

Tests are part of the architecture. New behavior should be tested at the layer where the rule lives.

Domain tests:

- Use `LibraryTemplate.Domain.Tests`.
- Test aggregate invariants, value objects, domain services, and domain events.
- Prefer simple stubs for domain repository ports.
- Assert `DomainException.Code` for business rule failures.

Application tests:

- Use `LibraryTemplate.Application.Tests`.
- Test validation, authorization ordering, orchestration, use-case responses, and save behavior.
- Use NSubstitute for application dependencies.
- Assert forbidden paths do not continue into business work or persistence.
- Use fixed clocks through `IClock`.

Persistence tests:

- Use `LibraryTemplate.Persistence.Tests`.
- Use PostgreSQL Testcontainers.
- Apply EF migrations against the container.
- Test EF mappings, repositories, projections, UUID v7 persistence, and UnitOfWork event dispatch.
- Do not use an in-memory EF provider for persistence behavior.

API integration tests:

- Use `LibraryTemplate.Api.IntegrationTests`.
- Use `WebApplicationFactory<Program>`.
- Use PostgreSQL Testcontainers.
- Replace Auth0 with the test authentication handler.
- Authenticate by setting the test subject header.
- Test HTTP status codes, ProblemDetails behavior, route flows, and app authorization from seeded database data.
- Disable background jobs in integration tests unless the test specifically targets worker behavior.

General test rules:

- Use xUnit, FluentAssertions, NSubstitute, and Testcontainers as established.
- Name tests in sentence-style underscores, for example `Child_cannot_borrow_adult_only_book`.
- Keep Arrange / Act / Assert structure.
- Add tests at the lowest layer that owns the behavior, plus API integration tests for important end-to-end flows.

Violates the rules:

- Testing persistence with EF in-memory when PostgreSQL behavior matters.
- Only testing a domain rule through the API.
- Skipping authorization tests for new protected use cases.
- Using real Auth0 in automated tests.
- Depending on the current system clock in deterministic tests.

## Naming and Code Style Rules

Follow the current C# style.

Rules:

- Target framework is centrally defined as `net10.0`.
- Nullable reference types stay enabled.
- Implicit usings stay enabled.
- Package versions are centrally managed in `Directory.Packages.props`.
- Use file-scoped namespaces.
- Prefer sealed classes unless inheritance is intended.
- Use primary constructors where consistent with the existing code.
- Use records for DTOs and simple immutable data shapes.
- Use async method names ending in `Async`.
- Use `IReadOnlyCollection<T>` for exposed collections.
- Use `[]` collection expressions where consistent with the existing code.
- Keep comments rare and useful; use them to clarify architecture or non-obvious sequencing.

Naming patterns:

- `I<Feature>AppService` and `<Feature>AppService`
- `<Action><Feature>AppRequest`
- `<Action><Feature>AppResponse`
- `<Action><Feature>ApiRequest`
- `<Action><Feature>ApiResponse`
- `<Concept>Repository`
- `I<Concept>Repository`
- `<Concept>Projection`
- `<EventName>Event`
- `<EventName>EventHandler`
- `<AdapterName>Options`
- `<LayerName>Services` for DI extension classes

Violates the rules:

- Introducing package versions in individual project files when central package management is used.
- Adding broad utility classes for unrelated behavior.
- Using public setters on domain state for convenience.
- Naming transport DTOs without the `Api` suffix.
- Naming use-case DTOs without the `App` suffix.

## Feature Implementation Checklist

When adding a new use case, follow this sequence:

1. Identify the domain rule owner.
2. Add or update Domain aggregate behavior, value objects, domain services, and domain events.
3. Add or update Domain repository ports only if aggregate access is needed by Domain/Application.
4. Add Application request/response contracts and validators.
5. Add or update the app service interface and implementation.
6. Enforce app-owned authorization in the app service.
7. Add or update Application dependency ports for projections or external services.
8. Implement persistence mappings, repositories, rows, and migrations.
9. Implement infrastructure adapters only behind Application ports.
10. Add API contracts and controller actions with explicit mapping.
11. Register new services in the correct layer's DI extension.
12. Add tests at the domain, application, persistence, and/or API layer as appropriate.

Before merging, confirm:

- No dependency direction was reversed.
- Controllers are thin.
- Domain invariants live in Domain.
- Application services use the standard orchestration flow.
- All writes commit through `IUnitOfWork`.
- Auth0 is used only for identity.
- App authorization uses database-backed roles and branch scope.
- DTO suffixes are correct.
- EF mappings use PostgreSQL-compatible explicit configuration.
- New options validate on startup.
- Logs are structured.
- Tests cover the layer that owns the behavior.

## Architecture Violations Checklist

The following patterns are not acceptable in this template:

- Business rules in controllers.
- Business rules in repositories.
- EF Core types in Domain or Application.
- ASP.NET Core MVC or HTTP types in Domain or Application services.
- Direct `DbContext` usage outside Persistence, except test setup.
- Direct `SaveChangesAsync()` from application services.
- Repository methods that save their own changes.
- API endpoints returning domain aggregates, EF entities, or persistence rows.
- Application services returning API DTOs.
- Controllers accepting application DTOs directly as request bodies.
- Domain aggregates injecting services.
- Auth0 claims used as app roles, permissions, or branch scope.
- `[Authorize(Roles = "...")]` used for app authorization.
- Enum integer persistence.
- Database-generated aggregate IDs.
- Domain events dispatched before successful persistence.
- New database technology added beside PostgreSQL.
- Unstructured console logging.
- Raw configuration reads scattered through business code.
- In-memory EF tests for repository behavior.
- Missing tests for new authorization paths.

If a future requirement appears to need one of these patterns, record an explicit architecture decision before implementing it.
