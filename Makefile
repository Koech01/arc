.PHONY: setup setup-dev up up-detached docker-build db dev build start logs ps stop down reset clean check-docker seed-demo

# First-time setup for Docker-based local runs
setup:
	@if [ -f .env ]; then echo "✔ .env already exists"; else cp .env.example .env && echo "✔ .env created from .env.example"; fi
	@echo "Run 'make up' to build and start Arc with Docker."

# Install local toolchain dependencies for active development
setup-dev: setup
	cd frontend && npm install
	cd backend && dotnet restore

check-docker:
	@docker info >/dev/null 2>&1 || (echo "Docker is not running. Start Docker Desktop, wait for it to finish, then retry."; exit 1)

# Build and start the full Dockerized app
up: setup check-docker
	docker compose up --build

up-detached: setup check-docker
	docker compose up -d --build
	@echo "Arc is starting at http://localhost:5266"

docker-build: setup check-docker
	docker compose build

# Start PostgreSQL only for local development
db:
	$(MAKE) check-docker
	docker compose up -d postgres
	@echo "Waiting for PostgreSQL..."
	@until [ "$$(docker inspect -f '{{.State.Health.Status}}' arc_mono_postgres 2>/dev/null)" = "healthy" ]; do printf "."; sleep 1; done; echo " ready"

# Run frontend (Vite dev server) and backend concurrently
dev: setup-dev db
	@command -v concurrently >/dev/null 2>&1 || npm install -g concurrently
	concurrently --names "api,ui" --prefix-colors "cyan,magenta" \
		"cd backend && dotnet run --project src/Arc.Api" \
		"cd frontend && npm run dev"

# Build frontend into backend/wwwroot, then publish backend
build:
	cd frontend && npm run build
	cd backend && dotnet publish src/Arc.Api -c Release -o ./publish

# Run the production build (serves frontend + API on :5266)
start:
	cd backend && dotnet ./publish/Arc.Api.dll

logs:
	docker compose logs -f

ps:
	docker compose ps

# Stop Docker containers
stop down: check-docker
	docker compose down

reset: check-docker
	docker compose down -v

# Reset demo workspace (idempotent — safe to run anytime)
seed-demo:
	dotnet run --project backend/scripts/seed-demo.csproj

# Remove build artifacts
clean:
	cd frontend && rm -rf node_modules dist
	cd backend && rm -rf publish src/Arc.Api/wwwroot