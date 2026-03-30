# Arc

A deterministic AI agent orchestrator - full-stack, single repo.

The React frontend is served by the .NET 8 API in production. In development both run independently with the Vite dev server proxying API calls.

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| Node.js | 18+ |
| Docker | any recent (for PostgreSQL) |

## Quick Start

```bash
git clone https://github.com/Koech01/arc arc && cd arc

# 1. Install dependencies and create .env
./setup.sh

# 2. Edit .env with your secrets (JWT key, etc.)
#    The defaults work for local development as-is.

# 3. Start PostgreSQL
make db

# 4. Start both servers
make dev
```

- Frontend (Vite): http://localhost:5173
- Backend (API + Swagger): http://localhost:5266
- Default admin: `admin@arc.com` / `Str0ng#Arc$99` - change after first login

## Project Structure

```
arc/
├── backend/    ← .NET 8 Clean Architecture API
└── frontend/   ← React + Vite + Tailwind SPA
```

## Development Workflow

The Vite dev server proxies `/api/*` to `http://localhost:5266`, so the frontend API base URL never needs changing during development.

```bash
make dev      # start frontend + backend concurrently
make db       # start PostgreSQL container
make stop     # stop PostgreSQL container
```

Work in `frontend/src` and `backend/src` as you normally would in each project.

## Production Build

```bash
make build    # builds frontend → backend/src/Arc.Api/wwwroot, then publishes .NET app
make start    # runs the published binary - serves API + SPA on :5266
```

The single binary at `backend/publish/Arc.Api.dll` serves everything.

## Configuration

Copy `.env.example` to `.env` and fill in the values. All backend settings follow the `Section__Key` convention and map directly to `appsettings.json`.

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__PostgreSQL` | Full Postgres connection string |
| `Jwt__SecretKey` | ≥32-character random string - generate with `openssl rand -base64 32` |
| `AdminAccount__Password` | Initial admin password - change after first login |
| `LLM__DefaultApiKey` | Optional default LLM provider key |

If PostgreSQL is unavailable the backend falls back to SQLite automatically.

## Docker

This project uses isolated Docker resources to avoid conflicts with other local environments.

| Resource | Name |
|----------|------|
| Container | `arc_mono_postgres` |
| Volume | `arc_mono_postgres_data` |
| Database | `arc_mono_db` |
| User | `arc_mono_user` |

```bash
make db       # start container
make stop     # stop container
make clean    # remove build artifacts (does not remove Docker volume)
```

To fully reset the database:

```bash
docker compose down -v   # stops container and removes volume
make db                  # recreates it fresh
```

## Running Tests

```bash
cd backend && dotnet test                          # all tests
cd backend && dotnet test tests/Arc.UnitTests      # unit only
cd backend && dotnet test tests/Arc.IntegrationTests
```

## Architecture

- [Backend architecture](backend/ARCHITECTURE.md)
- [Domain model](backend/DOMAIN_MODEL.md)
- [Frontend architecture](frontend/ARCHITECTURE.md)

## License

MIT - see [LICENSE](backend/LICENSE).