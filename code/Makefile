.DEFAULT_GOAL := no-default

no-default:
	$(error You must specify a make target)

migrate-data:
ifndef name
	$(error name is required. Usage: make migrate-data name=YourMigrationName)
endif
	dotnet ef migrations add $(name) --context DataContext --project backend/Cleanuparr.Persistence/Cleanuparr.Persistence.csproj --startup-project backend/Cleanuparr.Api/Cleanuparr.Api.csproj --output-dir Migrations/Data

migrate-events:
ifndef name
	$(error name is required. Usage: make migrate-events name=YourMigrationName)
endif
	dotnet ef migrations add $(name) --context EventsContext --project backend/Cleanuparr.Persistence/Cleanuparr.Persistence.csproj --startup-project backend/Cleanuparr.Api/Cleanuparr.Api.csproj --output-dir Migrations/Events