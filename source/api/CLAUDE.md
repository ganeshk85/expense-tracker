# Project: Expense Tracker — Backend API
Expense Tracker - Family Expense Intelligence Platform
This is the API folder for the .NET 10 back-end.

## Architecture
Clean Architecture with Modular Monolith pattern. Each domain module is a separate class library referenced by the host Web API project.

```
src/
  ExpenseTracker.Api/          -- ASP.NET Core Web API host (startup, DI, middleware)
  ExpenseTracker.Auth/         -- Auth module: login, sessions, MFA, invitations, RBAC
  ExpenseTracker.Receipt/      -- Receipt module: upload, storage, thumbnail trigger
  ExpenseTracker.OCR/          -- OCR module: job enqueue, result polling, expense hydration
  ExpenseTracker.Expense/      -- Expense module: CRUD, categories, tags, shared expenses
  ExpenseTracker.Budget/       -- Budget module: category budgets, alerts, reset
  ExpenseTracker.Search/       -- Search module: multi-field search, analytics
  ExpenseTracker.Shared/       -- Shared kernel: base entities, exceptions, interfaces
tests/
  ExpenseTracker.Auth.Tests/
  ExpenseTracker.Receipt.Tests/
  ...
```

## C# / .NET Conventions

- Target framework: `net10.0`
- Nullable reference types enabled in all projects
- Use `record` for DTOs/request-response objects
- Use `sealed` on classes that should not be subclassed
- Primary constructors preferred for service classes
- Pattern matching preferred over casting
- `IResult` returns from minimal API endpoints

## Module Structure (each module follows this pattern)
```
ModuleName/
  Endpoints/      -- Minimal API endpoint definitions
  Services/       -- Business logic (interfaces + implementations)
  Repositories/   -- Data access (interfaces + implementations)
  Entities/       -- EF Core entity classes
  Models/         -- Request/response records (DTOs)
  Exceptions/     -- Domain-specific exception types
  ModuleExtensions.cs  -- IServiceCollection extension to register the module
```

## Data Access
- EF Core 10 with PostgreSQL (Npgsql provider)
- Migrations in `ExpenseTracker.Api/Migrations/` — never modify tables manually
- Restore tools first (once per machine, from `source/api/`): `dotnet tool restore`
- New migration: `dotnet ef migrations add <Name> --project src/ExpenseTracker.Api --output-dir Migrations`
- Apply locally: `dotnet ef database update --project src/ExpenseTracker.Api`
- Schema is applied automatically on startup via `MigrateAsync()` — no manual step needed in Docker
- Use `AsNoTracking()` on all read-only queries
- Repository pattern: interfaces defined in module, implementations in same module

## API Design
- Minimal APIs only — no MVC controllers
- Group endpoints by module using `RouteGroupBuilder`
- All endpoints must require authentication unless explicitly marked `[AllowAnonymous]`
- Return `Results<T, ProblemHttpResult>` for typed responses

## Authentication & Security
- HTTP-only session cookies (no Bearer tokens in localStorage)
- Argon2id password hashing via `Konscious.Security.Cryptography`
- TOTP-based MFA via `OtpNet`
- RBAC enforced via ASP.NET Core Authorization policies
- Roles: `Owner`, `AdultMember`, `RestrictedMember`

## Logging
- Use `ILogger<T>` — never `Console.WriteLine`
- Structured logging with Serilog
- Log levels: `Debug` for dev, `Information` for prod

## Testing
- xUnit + FluentAssertions
- Integration tests: `WebApplicationFactory<Program>` with test PostgreSQL
- Unit tests: mock repositories with Moq
- Every new endpoint needs at least one happy-path integration test

## Commands
- `dotnet build` — build solution
- `dotnet test` — run all tests
- `dotnet run --project src/ExpenseTracker.Api` — start dev server
- `dotnet format` — format code

## Do NOT
- Do not use MVC controllers — use Minimal APIs only
- Do not store session tokens in localStorage or sessionStorage
- Do not use raw SQL — use EF Core repository layer
- Do not use `dynamic` or `object` as API return types
- Do not suppress nullable warnings with `!` without a comment explaining why
