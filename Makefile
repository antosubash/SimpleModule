.DEFAULT_GOAL := help
SHELL := /bin/bash

# ─────────────────────────────────────────────────
# SimpleModule — project task runner
# Run `make help` to see all available targets
# ─────────────────────────────────────────────────

HOST_PROJECT := template/SimpleModule.Host
APPHOST_PROJECT := SimpleModule.AppHost
DB_FILE := $(HOST_PROJECT)/app.db

# ─── Setup ───────────────────────────────────────

.PHONY: install
install: ## Install npm dependencies (all workspaces)
	npm install

.PHONY: restore
restore: ## Restore .NET packages
	dotnet restore

.PHONY: setup
setup: restore tool-restore install ## Full project setup (dotnet restore + tool restore + npm install)

.PHONY: tool-restore
tool-restore: ## Restore dotnet local tools (CSharpier, sm CLI)
	dotnet tool restore

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
	@[ -n "$(F)" ] || { echo "Usage: make test-filter F=ClassName or F=MethodName"; exit 1; }
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

# ─── Load Testing (k6) ──────────────────────────

K6_DIR := tests/k6

.PHONY: k6-smoke
k6-smoke: ## Run k6 smoke test (health endpoints)
	k6 run $(K6_DIR)/scenarios/health.js

.PHONY: k6-auth
k6-auth: ## Run k6 auth load test
	k6 run $(K6_DIR)/scenarios/auth.js

.PHONY: k6-products
k6-products: ## Run k6 products CRUD load test
	K6_PROFILE=load k6 run $(K6_DIR)/scenarios/products.js

.PHONY: k6-orders
k6-orders: ## Run k6 orders CRUD load test
	K6_PROFILE=load k6 run $(K6_DIR)/scenarios/orders.js

.PHONY: k6-pages
k6-pages: ## Run k6 page builder load test
	K6_PROFILE=load k6 run $(K6_DIR)/scenarios/pages.js

.PHONY: k6-page-lifecycle
k6-page-lifecycle: ## Run k6 full page lifecycle test (publish, tags, templates)
	k6 run $(K6_DIR)/scenarios/page-lifecycle.js

.PHONY: k6-settings
k6-settings: ## Run k6 settings and menu management load test
	k6 run $(K6_DIR)/scenarios/settings.js

.PHONY: k6-users
k6-users: ## Run k6 user management CRUD load test
	k6 run $(K6_DIR)/scenarios/users.js

.PHONY: k6-audit-logs
k6-audit-logs: ## Run k6 audit logs query, stats, and export test
	k6 run $(K6_DIR)/scenarios/audit-logs.js

.PHONY: k6-files
k6-files: ## Run k6 file upload/download load test
	k6 run $(K6_DIR)/scenarios/file-storage.js

.PHONY: k6-marketplace
k6-marketplace: ## Run k6 marketplace API load test (anonymous)
	k6 run $(K6_DIR)/scenarios/marketplace.js

.PHONY: k6-jobs
k6-jobs: ## Run k6 background jobs load test
	k6 run $(K6_DIR)/scenarios/background-jobs.js

.PHONY: k6-feature-flags
k6-feature-flags: ## Run k6 feature flags CRUD load test
	k6 run $(K6_DIR)/scenarios/feature-flags.js

.PHONY: k6-tenants
k6-tenants: ## Run k6 tenants CRUD load test
	k6 run $(K6_DIR)/scenarios/tenants.js

.PHONY: k6-mixed
k6-mixed: ## Run k6 mixed traffic load test (realistic simulation)
	k6 run $(K6_DIR)/scenarios/mixed.js

.PHONY: k6-hotspots
k6-hotspots: ## Run k6 hotspot detection (all endpoints, sorted by latency)
	@mkdir -p $(K6_DIR)/results
	k6 run $(K6_DIR)/scenarios/hotspots.js

.PHONY: k6-stress
k6-stress: ## Run k6 stress test (mixed scenario, high load)
	K6_PROFILE=stress k6 run $(K6_DIR)/scenarios/mixed.js

.PHONY: k6-spike
k6-spike: ## Run k6 spike test (sudden traffic burst)
	K6_PROFILE=spike k6 run $(K6_DIR)/scenarios/mixed.js

.PHONY: k6-all
k6-all: k6-smoke k6-auth k6-products k6-orders k6-pages k6-page-lifecycle k6-settings k6-users k6-audit-logs k6-files k6-marketplace k6-jobs k6-feature-flags k6-tenants k6-mixed ## Run all k6 load test scenarios

.PHONY: load
load: k6-all ## Alias: run all k6 load tests

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
lint: ## Lint JS (Biome) and C# (CSharpier)
	$(MAKE) lint-js
	$(MAKE) lint-cs

.PHONY: lint-js
lint-js: ## Run Biome linter on JS/TS
	npm run lint

.PHONY: lint-cs
lint-cs: ## Run CSharpier format check on C#
	dotnet csharpier check .

.PHONY: format
format: ## Format JS (Biome) and C# (CSharpier)
	$(MAKE) format-js
	$(MAKE) format-cs

.PHONY: format-js
format-js: ## Run Biome formatter (writes changes)
	npm run format

.PHONY: format-cs
format-cs: ## Run CSharpier formatter (writes changes)
	dotnet csharpier format .

.PHONY: check
check: ## Run Biome check + CSharpier check + page validation
	npm run check
	dotnet csharpier check .

.PHONY: check-fix
check-fix: ## Auto-fix Biome + CSharpier formatting issues
	npm run check:fix
	dotnet csharpier format .

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

# ─── CI ─────────────────────────────────────────

.PHONY: ci
ci: check build test ## Run what CI runs: lint, build, test

.PHONY: ci-full
ci-full: check build-all test-all ## Full CI: lint, build (.NET + JS), all tests

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

define HELP_HEADER
\n\033[1m SimpleModule\033[0m — available targets\n
endef

.PHONY: help
help: ## Show this help
	@printf "$(HELP_HEADER)"
	@awk ' \
	/^# .+ Setup|^# .+ Build|^# .+ Run|^# .+ Test|^# .+ Load Test|^# .+ Database|^# .+ Code Qual|^# .+ Code Gen|^# .+ CI|^# .+ Docker|^# .+ Clean|^# .+ Help/ { \
		section = $$0; \
		gsub(/^# [^A-Z]*/, "", section); \
		gsub(/ [^A-Z]*$$/, "", section); \
		printf "\n\033[33m %s\033[0m\n", section; \
		next \
	} \
	/^[a-zA-Z0-9_-]+:.*## / { \
		target = $$0; sub(/:.*/, "", target); \
		desc = $$0; sub(/.*## /, "", desc); \
		printf "  \033[36m%-20s\033[0m %s\n", target, desc \
	}' $(MAKEFILE_LIST)
	@printf "\n"
