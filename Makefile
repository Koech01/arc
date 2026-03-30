.PHONY: setup dev build start db stop clean

# First-time setup
setup:
	@[ -f .env ] || cp .env.example .env && echo "✔ .env created - edit it before continuing"
	cd frontend && npm install
	cd backend && dotnet restore

# Start PostgreSQL
db:
	docker compose up -d
	@echo "Waiting for PostgreSQL..." && sleep 3

# Run frontend (Vite dev server) and backend concurrently
dev:
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

# Stop PostgreSQL
stop:
	docker compose down

# Remove build artifacts
clean:
	cd frontend && rm -rf node_modules dist
	cd backend && rm -rf publish src/Arc.Api/wwwroot
