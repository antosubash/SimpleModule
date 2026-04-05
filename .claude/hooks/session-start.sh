#!/bin/bash
set -euo pipefail

# Only run in remote (cloud) environments
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

cd "$CLAUDE_PROJECT_DIR"

# Install .NET SDK if not available
if ! command -v dotnet &> /dev/null; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel 10.0
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$DOTNET_ROOT:$PATH"
  echo "export DOTNET_ROOT=\"$HOME/.dotnet\"" >> "$CLAUDE_ENV_FILE"
  echo "export PATH=\"$DOTNET_ROOT:\$PATH\"" >> "$CLAUDE_ENV_FILE"
fi

# Install Docker if not available
if ! command -v docker &> /dev/null; then
  curl -fsSL https://get.docker.com | sh
fi

# Install .NET dependencies and tools
dotnet restore
dotnet tool restore

# Install Node.js dependencies (npm workspaces)
npm install

# Install Playwright browsers for e2e tests
npx -w tests/e2e playwright install --with-deps