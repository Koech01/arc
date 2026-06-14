# Arc

A deterministic AI agent orchestrator - full-stack, single repo.

Arc runs as a Dockerized full-stack app: PostgreSQL, the .NET 8 API, and the built React frontend are started together with Docker Compose. For active development, the frontend can also run separately with Vite proxying API calls.

## Prerequisites

| Tool | Version |
|------|---------|
| Docker Desktop | any recent |

.NET 8 and Node.js 18+ are only needed for non-Docker local development.

## Quick Start

> [!IMPORTANT]
> Before starting, open Docker Desktop or ensure the Docker daemon is running. If Docker is not running, setup may fail with connection errors, failed builds, or application startup issues.

```bash
git clone https://github.com/Koech01/arc arc && cd arc

# Create .env and verify Docker Desktop is running
./setup.sh

# Build and start the app + PostgreSQL
make up
```

Open `http://localhost:5266`.

Default admin:

```text
admin@arc.com
Str0ng#Arc$99
```

Use `Ctrl+C` to stop the foreground Docker logs, or run `make up-detached` to start the stack in the background.

## Project Structure

```
arc/
├── backend/    ← .NET 8 Clean Architecture API
└── frontend/   ← React + Vite + Tailwind SPA
```

## Development Workflow

The Vite dev server proxies `/api/*` to `http://localhost:5266`, so the frontend API base URL never needs changing during development.

```bash
make setup-dev  # install frontend and backend dependencies
make dev        # start PostgreSQL in Docker, then local API + Vite
make stop       # stop Docker containers
```

Work in `frontend/src` and `backend/src` as you normally would in each project.

## Local Production Build

```bash
make build    # builds frontend → backend/src/Arc.Api/wwwroot, then publishes .NET app
make start    # runs the published binary - serves API + SPA on :5266
```

The single binary at `backend/publish/Arc.Api.dll` serves everything.

## Configuration

`./setup.sh` creates `.env` from `.env.example` if it does not already exist. The defaults are intended for local Docker evaluation. All backend settings follow the `Section__Key` convention and map directly to `appsettings.json`.

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__PostgreSQL` | Full Postgres connection string |
| `Jwt__SecretKey` | JWT signing key; demo default works locally, replace for real deployments |
| `AdminAccount__Password` | Initial admin password - change after first login |
| `LLM__DefaultApiKey` | Optional default LLM provider key |

PostgreSQL is required and is started by Docker Compose.

## Docker

This project uses isolated Docker resources to avoid conflicts with other local environments.

| Resource | Name |
|----------|------|
| App container | `arc_mono_app` |
| Container | `arc_mono_postgres` |
| Volume | `arc_mono_postgres_data` |
| Database | `arc_mono_db` |
| User | `arc_mono_user` |

```bash
make up          # build and start app + PostgreSQL
make up-detached # start in the background
make logs        # follow container logs
make ps          # show container status
make stop        # stop containers
make reset       # stop containers and remove the database volume
```

To fully reset the database:

```bash
make reset
make up
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