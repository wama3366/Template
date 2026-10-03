# LibraryTemplate

A narrow ASP.NET Core DDD template using Auth0 authentication, app-owned authorization, EF Core/Postgres, UUID v7 ids, FluentValidation, ProblemDetails, Serilog, immediate domain events, and Testcontainers-backed tests.

## Local Setup

Start Postgres:

```powershell
docker compose up -d
```

Apply migrations:

```powershell
dotnet ef database update --project LibraryTemplate.Persistence --startup-project LibraryTemplate.Api
```

Run the API:

```powershell
dotnet run --project LibraryTemplate.Api
```

## Seeded Auth0 Subjects

These subjects are seeded so local/dev tokens can map to app users:

- `auth0|adult-borrower`
- `auth0|child-borrower`
- `auth0|branch-admin`
- `auth0|system-admin`

Auth0 proves identity. The application database decides user roles and branch scope.

## Main Flows

- `POST /branches/{branchId}/loans`
- `GET /branches/{branchId}/loans/borrowed`
- `GET /loans/borrowed`
- `POST /books/{bookId}/block`
- `POST /branches/{branchId}/delete`

The seeded branch id is `01971f00-0000-7000-8000-000000000001`.

## DTO Naming

API contracts are transport-facing and use `*ApiRequest` / `*ApiResponse`.
Application contracts are use-case-facing and use `*AppRequest` / `*AppResponse`.
Controllers map between them explicitly.
