.DEFAULT_GOAL := help
SHELL := /bin/bash

# ─────────────────────────────────────────────────
# SimpleModule — project task runner
# Run `make help` to see all available targets
# ─────────────────────────────────────────────────

HOST_PROJECT := template/SimpleModule.Host
APPHOST_PROJECT := SimpleModule.AppHost
CLI_PROJECT := cli/SimpleModule.Cli
DB_FILE := $(HOST_PROJECT)/app.db

# ─── Setup ───────────────────────────────────────

.PHONY: install
install: ## Install npm dependencies (all workspaces)
	npm install

.PHONY: restore
restore: ## Restore .NET packages
	dotnet restore

.PHONY: setup
setup: restore install ## Full project setup (dotnet restore + npm install)

# ─── Build ───────────────────────────────────────

.PHONY: build
build: ## Build the .NET solution
	dotnet build

.PHONY: build-release
build-release: ## Build in Release configuration
	dotnet build -c Release

.PHONY: build-js
build-js: ## Production build for all JS (minified, optimized)
	npm run build

.PHONY: build-js-dev
build-js-dev: ## Dev build for all JS (unminified, source maps)
	npm run build:dev

.PHONY: build-all
build-all: build build-js ## Build .NET solution + production JS

.PHONY: publish
publish: ## Publish the host project for production
	dotnet publish $(HOST_PROJECT)/SimpleModule.Host.csproj -c Release

# ─── Run ─────────────────────────────────────────

.PHONY: dev
dev: ## Start full dev environment (dotnet + JS watches)
	npm run dev

.PHONY: run
run: ## Run the host app (https://localhost:5001)
	dotnet run --project $(HOST_PROJECT)

.PHONY: run-watch
run-watch: ## Run with dotnet watch (hot reload)
	dotnet watch run --project $(HOST_PROJECT)

.PHONY: aspire
aspire: ## Run via Aspire AppHost (orchestrated with PostgreSQL)
	dotnet run --project $(APPHOST_PROJECT)

# ─── Test ────────────────────────────────────────

.PHONY: test
test: ## Run all .NET tests
	dotnet test

.PHONY: test-filter
test-filter: ## Run filtered tests (usage: make test-filter F=ClassName)
	dotnet test --filter "FullyQualifiedName~$(F)"

.PHONY: test-verbose
test-verbose: ## Run all tests with detailed output
	dotnet test --verbosity normal

.PHONY: test-e2e
test-e2e: ## Run Playwright end-to-end tests
	npm run test:e2e

.PHONY: test-e2e-ui
test-e2e-ui: ## Run Playwright tests with UI mode
	npm run test:e2e:ui

.PHONY: test-e2e-headed
test-e2e-headed: ## Run Playwright tests in headed browser
	cd tests/e2e && npx playwright test --headed

.PHONY: test-e2e-smoke
test-e2e-smoke: ## Run only e2e smoke tests
	cd tests/e2e && npm run test:smoke

.PHONY: test-e2e-flows
test-e2e-flows: ## Run only e2e flow tests
	cd tests/e2e && npm run test:flows

.PHONY: test-e2e-report
test-e2e-report: ## View Playwright HTML test report
	cd tests/e2e && npm run report

.PHONY: test-all
test-all: test test-e2e ## Run all .NET and e2e tests

# ─── Database ────────────────────────────────────

.PHONY: db-reset
db-reset: ## Delete the SQLite database and recreate on next run
	@echo "Removing SQLite database..."
	rm -f $(DB_FILE) $(DB_FILE)-shm $(DB_FILE)-wal
	@echo "Database removed. It will be recreated on next app start (EnsureCreated)."

.PHONY: db-reset-docker
db-reset-docker: ## Reset the Docker PostgreSQL database (destroys volume)
	docker compose down -v
	docker compose up -d postgres
	@echo "PostgreSQL volume destroyed and container restarted."

# ─── Code Quality ────────────────────────────────

.PHONY: lint
lint: ## Run Biome linter
	npm run lint

.PHONY: format
format: ## Run Biome formatter (writes changes)
	npm run format

.PHONY: check
check: ## Run Biome lint + format check + page validation
	npm run check

.PHONY: check-fix
check-fix: ## Auto-fix Biome lint + formatting issues
	npm run check:fix

.PHONY: validate-pages
validate-pages: ## Validate C# endpoints have matching TS page entries
	npm run validate-pages

# ─── Code Generation ────────────────────────────

.PHONY: generate-types
generate-types: ## Extract TypeScript types from source-generated DTOs
	npm run generate:types

.PHONY: ui-add
ui-add: ## Add a Radix UI component (interactive)
	npm run ui:add

# ─── CLI (sm tool) ───────────────────────────────

.PHONY: cli-install
cli-install: ## Install the sm CLI tool locally
	dotnet pack $(CLI_PROJECT)/SimpleModule.Cli.csproj -c Release
	dotnet tool install --global --add-source $(CLI_PROJECT)/nupkg SimpleModule.Cli || \
	dotnet tool update --global --add-source $(CLI_PROJECT)/nupkg SimpleModule.Cli

.PHONY: doctor
doctor: ## Run sm doctor to validate project structure
	sm doctor

.PHONY: doctor-fix
doctor-fix: ## Run sm doctor with auto-fix
	sm doctor --fix

# ─── Docker ──────────────────────────────────────

.PHONY: docker-build
docker-build: ## Build the Docker image
	docker build -t simplemodule .

.PHONY: docker-up
docker-up: ## Start all Docker Compose services
	docker compose up -d

.PHONY: docker-down
docker-down: ## Stop all Docker Compose services
	docker compose down

.PHONY: docker-logs
docker-logs: ## Tail Docker Compose logs
	docker compose logs -f

.PHONY: docker-ps
docker-ps: ## Show running Docker Compose services
	docker compose ps

# ─── Clean ───────────────────────────────────────

.PHONY: clean
clean: ## Clean .NET build outputs
	dotnet clean
	find . -type d \( -name bin -o -name obj \) -not -path './node_modules/*' -exec rm -rf {} + 2>/dev/null || true

.PHONY: clean-js
clean-js: ## Remove node_modules and JS build outputs
	rm -rf node_modules
	find . -name 'node_modules' -type d -not -path './node_modules/*' -exec rm -rf {} + 2>/dev/null || true

.PHONY: clean-all
clean-all: clean clean-js db-reset ## Full clean (.NET + JS + database)
	@echo "All build artifacts and database removed."

.PHONY: pristine
pristine: clean-all setup ## Clean everything and reinstall from scratch

# ─── Help ────────────────────────────────────────

.PHONY: help
help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m  %-20s\033[0m %s\n", $$1, $$2}' | \
		sort
