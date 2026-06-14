#!/usr/bin/env bash
set -e

echo "── Arc Setup ──"

if [ ! -f .env ]; then
  cp .env.example .env
  echo "✔ .env created from .env.example"
else
  echo "✔ .env already exists"
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "✖ Docker is not installed or not on PATH."
  echo "  Install Docker Desktop, then run ./setup.sh again."
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "✖ Docker is installed but not running."
  echo "  Start Docker Desktop, wait until it says Docker is running, then run ./setup.sh again."
  exit 1
fi

echo "✔ Docker is running"

echo ""
echo "Setup complete. Next steps:"
echo "  make up           # build and start the Dockerized app"
echo ""
echo "Then open:"
echo "  http://localhost:5266"
echo ""
echo "For local source development with Node and .NET installed:"
echo "  make dev"