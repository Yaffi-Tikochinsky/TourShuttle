# TourShuttle: project rules

ASP.NET Core Web API (.NET 10) for booking seats on pre-arranged shuttles to tour sites. This is a course final project; the teacher's checklist is the source of truth for what must exist.

## Communication
- Reply to the user in Hebrew. Code, identifiers and commands stay in English.
- Before executing a stage, explain the plan when asked. After each stage, explain every new file and why it exists.

## Architecture (Clean Architecture)
- `TourShuttle.Domain`: entities and enums only. No package references, no EF Core.
- `TourShuttle.Application`: interfaces (repositories, IUnitOfWork, services), DTOs, services, AutoMapper profiles, Result types. References Domain only. Must not reference EF Core.
- `TourShuttle.Infrastructure`: AppDbContext, Fluent configurations, repositories, migrations, seed, JWT, password hashing. References Application and Domain.
- `TourShuttle.API`: controllers, middleware, Program.cs. Uses Infrastructure only in Program.cs for DI registration.
- `TourShuttle.Tests`: xUnit + Moq for services; SQLite in-memory for the two-DbContext concurrency test.

## Rules
- Controllers never inject DbContext or repositories; they call Application services only.
- Async all the way with a `CancellationToken` passed through every layer. Never `.Result` or `.Wait()`.
- Entities never leave or enter through the API: request/response DTOs mapped with AutoMapper.
- Business validation that depends on data lives in services, not controllers.
- Read-only queries use `AsNoTracking`; pagination uses `Skip`/`Take` inside the query; no N+1 (use `Include`/`ThenInclude`).
- DateTime is UTC everywhere (`DateTime.UtcNow`); convert to Israel time only for display or time-band rules.
- No secrets in code or in the repo: connection strings and JWT keys come from user-secrets (dev) or environment variables (Render).
- Never log passwords, tokens or register/login request bodies.
- Concurrency: `Tour.Version` is a `[ConcurrencyCheck] Guid` renewed in `SaveChangesAsync`; Infrastructure translates `DbUpdateConcurrencyException` into `ConcurrencyConflictException`, and the API returns 409.

## Commands (run from the repo root)
- Build: `dotnet build`
- Test: `dotnet test`
- Run API: `dotnet run --project src/TourShuttle.API` (Swagger at `/swagger`)
- Migrations: `dotnet ef migrations add <Name> -p src/TourShuttle.Infrastructure -s src/TourShuttle.API`
- Local DB: Docker container `tourshuttle-pg` (PostgreSQL 16, bound to 127.0.0.1:5432)
