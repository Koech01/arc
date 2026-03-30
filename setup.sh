#!/usr/bin/env bash
set -e

echo "── Arc Setup ──"

# .env
if [ ! -f .env ]; then
  cp .env.example .env
  echo "✔ .env created from .env.example"
  echo "  → Edit .env with your secrets before running the app"
fi

# Node deps
echo "Installing frontend dependencies..."
(cd frontend && npm install)

# .NET deps
echo "Restoring backend dependencies..."
(cd backend && dotnet restore)

echo ""
echo "Setup complete. Next steps:"
echo "  1. Edit .env (JWT key, admin password, etc.)"
echo "  2. make db        # start PostgreSQL (requires Docker)"
echo "  3. make dev       # start both servers"
echo "  OR"
echo "  3. make build && make start   # production"