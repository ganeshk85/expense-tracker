# Expense Tracker — Local Development Setup

A privacy-first, self-hosted expense and budget management platform for families. All OCR and AI processing runs locally — no data leaves the deployment.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js (React, TypeScript, Tailwind CSS) |
| Backend | .NET 10 ASP.NET Core Web API |
| OCR Worker | Python FastAPI + Tesseract + OpenCV |
| Database | PostgreSQL 17 |
| Cache / Queue | Redis 7 |

---

## Prerequisites

Install the following before getting started:

- **Docker** — required for the full stack (includes Docker Compose). See options below.
- [Git](https://git-scm.com/)

For running individual services outside Docker:
- [Node.js 20+](https://nodejs.org/) — frontend
- [.NET 10 SDK](https://dotnet.microsoft.com/download) — backend API
- [Python 3.11+](https://www.python.org/) — OCR worker
- [PostgreSQL 17](https://www.postgresql.org/download/) — database
- Redis — see platform-specific instructions below

### Redis on Windows (without Docker)

Redis does not have an official Windows build. The recommended option for local development on Windows is **Memurai**, a Redis-compatible server for Windows.

**Install Memurai:**

1. Download the installer from [memurai.com](https://www.memurai.com/)
2. Run the installer — Memurai registers itself as a Windows service named `Memurai`
3. The service starts automatically on boot; no configuration is needed for local development

**Verify Memurai is running:**

```powershell
Get-Service -Name Memurai
```

The `Status` column should show `Running`. If not, start it with:

```powershell
Start-Service -Name Memurai
```

Memurai listens on `localhost:6379` by default, which matches the connection string in `appsettings.Development.json`.

### Inspecting the OCR Job Queue (Redis Streams)

The application uses two Redis streams for OCR processing:

| Stream | Direction | Consumer group |
|---|---|---|
| `ocr.jobs` | API → OCR worker | `ocr-workers` |
| `ocr.results` | OCR worker → API | `api-consumer` |

Open the Memurai CLI from a PowerShell prompt:

```powershell
& "C:\Program Files\Memurai\memurai-cli.exe"
```

**View pending (unprocessed) jobs:**

```
XPENDING ocr.jobs ocr-workers - + 10
```

**View all entries in the jobs stream (last 10):**

```
XRANGE ocr.jobs - + COUNT 10
```

**View all entries in the results stream (last 10):**

```
XRANGE ocr.results - + COUNT 10
```

**Check how many entries are in each stream:**

```
XLEN ocr.jobs
XLEN ocr.results
```

**Inspect consumer group state (lag, last-delivered ID):**

```
XINFO GROUPS ocr.jobs
XINFO GROUPS ocr.results
```

A `pending` count greater than zero on `ocr.jobs` means the OCR worker has received jobs that have not been acknowledged — the worker may be down or processing slowly. A non-zero `pending` count on `ocr.results` means the API consumer has not yet processed results from the worker.

### Docker on macOS

**Option A — Colima (free, recommended for Mac)**

[Colima](https://github.com/abiosoft/colima) is a lightweight Docker runtime for macOS that does not require Docker Desktop. Install it via Homebrew:

```bash
brew install colima docker docker-compose
colima start
```

Run `colima start` once after each reboot before using Docker. To have it start automatically at login:

```bash
brew services start colima
```

**Option B — Docker Desktop**

[Docker Desktop](https://www.docker.com/products/docker-desktop/) is the official GUI client. Download and install it, then launch it before running any Docker commands.

---

## Repository Structure

```
expense-tracker/
├── docker-compose.yml
├── source/
│   ├── web/          # Next.js frontend
│   ├── api/          # .NET 10 backend API
│   └── ocr/          # Python OCR worker
├── documents/        # BRD, architecture docs, sprint plans
└── infrastructure/   # Infrastructure config
```

---

## Running the Full Stack (Recommended)

From the project root, run:

```bash
docker compose up --build
```

This builds and starts all services. On first run, Docker will pull base images and build each service — this takes a few minutes. Subsequent starts are faster.

### Service URLs

| Service | URL | Description |
|---|---|---|
| Frontend | http://localhost:3000 | Next.js web app |
| Backend API | http://localhost:5000 | ASP.NET Core REST API |
| PostgreSQL | localhost:5432 | Database (user: `postgres`, password: `postgres`, db: `expense_tracker`) |
| Redis | localhost:6379 | Cache and job queue (internal) |

> The OCR worker has no exposed port — it processes jobs from the Redis queue automatically.

### Run in the background

```bash
docker compose up --build -d
```

### View logs

```bash
docker compose logs -f              # all services
docker compose logs -f api          # API only
docker compose logs -f ocr-worker   # OCR worker only
```

### Stop all services

```bash
docker compose down
```

### Full reset (wipes all data)

```bash
docker compose down -v
```

> **Warning:** `-v` removes all Docker volumes including the database and uploaded files.

---

## Running Individual Services (for active development)

Use this when you are actively working on one service and want hot-reload without rebuilding Docker images.

> Postgres and Redis must still be running. Start them first:
> ```bash
> docker compose up postgres redis -d
> ```

### Frontend

```bash
cd source/web
npm install
npm run dev
```

Runs at http://localhost:3000 with hot-reload.

### Backend API

```bash
cd source/api
dotnet run --project src/ExpenseTracker.Api
```

Runs at http://localhost:5000. Set environment variables as needed (see `docker-compose.yml` for reference values).

### OCR Worker

```bash
cd source/ocr
pip install -e .
python -m src.main
```

Connects to Redis at `localhost:6379` by default.

---

## Environment Variables

All environment variables are pre-configured in `docker-compose.yml` for local development. No `.env` file is needed to get started.

| Variable | Service | Value (local) |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | api | `Host=postgres;Database=expense_tracker;Username=postgres;Password=postgres` |
| `ConnectionStrings__Redis` | api | `redis:6379` |
| `REDIS_URL` | ocr-worker | `redis://redis:6379` |
| `NEXT_PUBLIC_API_URL` | web | `http://localhost:5000` |

---

## Data Storage

Uploaded files are persisted in a Docker volume (`storage_data`) mapped to `/storage` inside containers:

```
/storage
  /receipts       # uploaded receipt images/PDFs
  /attachments    # other expense attachments
  /ocr-json       # raw OCR output (retained for audit)
  /thumbnails     # generated image thumbnails
```

---

## First-Time Login (Dev Credentials)

There is no pre-seeded user. On first run you must seed the Owner account directly into the database.

**Step 1 — Start the stack:**
```bash
docker compose up -d
```

**Step 2 — Insert the dev Owner account:**
```bash
docker exec -i expense-tracker-postgres-1 psql -U postgres -d expense_tracker << 'EOF'
INSERT INTO "Users" ("Id", "Username", "PasswordHash", "Role", "IsActive", "MfaEnabled", "TotpSecretEncrypted", "FailedLoginAttempts", "LockedUntil", "LastLoginAt", "CreatedAt", "UpdatedAt")
VALUES (
  gen_random_uuid(),
  'admin',
  '3q2+78r+ur4BAgMEBQYHCA==:nYJgU7Xla6zoNQ3rrZvJ9PoScoGDnwzwogz+w1ojxTE=',
  'Owner',
  true,
  false,
  NULL,
  0,
  NULL,
  NULL,
  now(),
  now()
);
EOF
```

**Step 3 — Log in at http://localhost:3000/login:**

| Field | Value |
|---|---|
| Username | `admin` |
| Password | `Admin@123` |

> These credentials are for local development only. Change the password immediately in any shared or staging environment.

**Swagger (API testing without the frontend)**

The full API is browsable at http://localhost:5000/swagger. Call `POST /auth/login` with the credentials above to establish a session, then use any other endpoint.

---

## Database Access (PostgreSQL)

Connect using any PostgreSQL client with these credentials:

| Field | Value |
|---|---|
| Host | `localhost` |
| Port | `5432` |
| Database | `expense_tracker` |
| Username | `postgres` |
| Password | `postgres` |

**CLI (no install needed):**
```bash
docker exec -it expense-tracker-postgres-1 psql -U postgres -d expense_tracker
```

Once connected:
```sql
\dt                -- list all tables
\d "Users"         -- describe a table
SELECT * FROM "Users";
\q                 -- quit
```

**GUI — [TablePlus](https://tableplus.com/)** (free tier, Mac native): create a new PostgreSQL connection using the credentials above.

---

## Common Issues

**Port already in use**
If `3000`, `5000`, `5432`, or `6379` are occupied, stop the conflicting process or change the host port mapping in `docker-compose.yml`.

**Docker build fails on first run**
Ensure your Docker runtime is running (Docker Desktop or `colima start`) and you have an active internet connection for pulling base images.

**Database connection errors on API startup**
The API waits for Postgres to be healthy before starting. If it still fails, run `docker compose down -v` and restart — a corrupted volume initialisation is the usual cause.

**OCR worker not processing jobs**
Check that the `api` service started successfully first — the worker depends on it. Run `docker compose logs ocr-worker` for details.

---

## Development Workflow

1. Create a branch: `feature/<name>`, `fix/<name>`, or `chore/<name>`
2. Make changes and write tests for any new functionality
3. Open a PR targeting `dev` — never push directly to `main`
4. Use conventional commits: `feat:`, `fix:`, `chore:`, `docs:`, `test:`

See [documents/sprint-plan.md](documents/sprint-plan.md) for current sprint tasks and [documents/user-stories.md](documents/user-stories.md) for feature requirements.

---

## Database Migrations

EF Core migrations version-control all schema changes. The `dotnet-ef` tool is pinned to v10.0.0 via a local tool manifest — no global install needed.

### One-time setup (per developer machine)

From `source/api/`:
```bash
dotnet tool restore
```

### Adding a migration (after changing an entity)

Run from `source/api/`:
```bash
dotnet ef migrations add <MigrationName> \
  --project src/ExpenseTracker.Api \
  --output-dir Migrations
```

Use descriptive PascalCase names: `AddExpenseTable`, `AddBudgetAlerts`, `AddReceiptTagsIndex`.

Review the generated `Up()` method in `src/ExpenseTracker.Api/Migrations/`, then commit all three generated files (`<timestamp>_<Name>.cs`, `<timestamp>_<Name>.Designer.cs`, `AppDbContextModelSnapshot.cs`). On next `docker compose up`, `MigrateAsync()` applies it automatically.

> If .NET SDK is not installed locally, run migrations inside a Docker SDK container:
> ```bash
> docker run --rm \
>   -v $(pwd)/source/api:/app -w /app \
>   mcr.microsoft.com/dotnet/sdk:10.0 \
>   bash -c "dotnet tool restore && dotnet restore && dotnet ef migrations add <MigrationName> --project src/ExpenseTracker.Api --output-dir Migrations"
> ```

### Applying migrations manually (outside Docker)

```bash
dotnet ef database update --project src/ExpenseTracker.Api \
  --connection "Host=localhost;Database=expense_tracker;Username=postgres;Password=postgres"
```

### Rolling back (development only)

```bash
dotnet ef database update <PreviousMigrationName> --project src/ExpenseTracker.Api
dotnet ef migrations remove --project src/ExpenseTracker.Api
```

Never roll back in a shared environment — write a new forward migration instead.

### Adding entities from new modules (Expense, Budget, Search)

When a new module is built:
1. Define the entity in the module project (e.g. `ExpenseTracker.Expense/Entities/Expense.cs`)
2. Add a `DbSet<Expense>` to `AppDbContext` and configure it in `OnModelCreating`
3. Add a `ProjectReference` in `ExpenseTracker.Api.csproj`
4. Run `dotnet ef migrations add AddExpenseTable --project src/ExpenseTracker.Api --output-dir Migrations`
5. Commit the three generated files — `MigrateAsync()` applies on next startup
