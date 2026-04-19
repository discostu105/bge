#!/bin/bash

# BGE Development Server Startup Script
# Starts the local development environment using Docker Compose
# 
# Usage:
#   ./dev.sh              # Auto-detect mode (GitHub OAuth if creds present, else DevAuth)
#   ./dev.sh --dev-auth   # Force DevAuth mode (password-less login)
#   ./dev.sh --github     # Require GitHub OAuth credentials

set -e

MODE="auto"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --dev-auth)
            MODE="devauth"
            shift
            ;;
        --github)
            MODE="github"
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--dev-auth | --github]"
            exit 1
            ;;
    esac
done

# Navigate to src directory (works from anywhere)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC_DIR="$SCRIPT_DIR/src"

if [ ! -d "$SRC_DIR" ]; then
    echo "❌ Error: src/ directory not found at $SRC_DIR"
    exit 1
fi

cd "$SRC_DIR"

# Determine which mode to use
if [ "$MODE" = "auto" ]; then
    if [ -n "$GITHUB_CLIENT_ID" ] && [ -n "$GITHUB_CLIENT_SECRET" ]; then
        MODE="github"
    else
        MODE="devauth"
    fi
fi

# Find the correct docker-compose command (docker compose vs docker-compose)
if command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE_CMD="docker-compose"
elif command -v docker &> /dev/null && docker compose version &> /dev/null; then
    DOCKER_COMPOSE_CMD="docker compose"
else
    echo "❌ Error: docker-compose is not installed"
    echo "Install Docker Desktop or the Docker CLI with compose plugin"
    exit 1
fi

# Start server based on mode
if [ "$MODE" = "devauth" ]; then
    echo "🚀 Starting BGE dev server with DevAuth (password-less login)..."
    export Bge__DevAuth=true
    # DevAuth requires placeholder GitHub creds to pass startup validation
    export GITHUB_CLIENT_ID="${GITHUB_CLIENT_ID:-dev-placeholder}"
    export GITHUB_CLIENT_SECRET="${GITHUB_CLIENT_SECRET:-dev-placeholder}"
    $DOCKER_COMPOSE_CMD up --build
else
    # GitHub OAuth mode
    if [ -z "$GITHUB_CLIENT_ID" ]; then
        echo "❌ Error: GITHUB_CLIENT_ID not set. Either set it or use --dev-auth flag"
        exit 1
    fi
    if [ -z "$GITHUB_CLIENT_SECRET" ]; then
        echo "❌ Error: GITHUB_CLIENT_SECRET not set. Either set it or use --dev-auth flag"
        exit 1
    fi
    echo "🚀 Starting BGE dev server with GitHub OAuth..."
    $DOCKER_COMPOSE_CMD up --build
fi
