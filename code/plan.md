 # Cleanuperr Clean Architecture Migration Plan

## Overview
This plan migrates the existing Cleanuperr .NET application from a mixed architecture to a clean architecture following Domain-Driven Design principles.

## Target Architecture

### Project Structure
```
Cleanuperr.Domain/        # Core business logic (no dependencies)
├── Entities/             # Business entities
├── ValueObjects/         # Value objects
├── Enums/               # Domain enums
├── Events/              # Domain events
├── Exceptions/          # Domain-specific exceptions
└── Services/            # Domain services

Cleanuperr.Application/   # Use cases and application logic
├── Features/            # Feature-based organization (CQRS)
│   ├── {FeatureName}/
│   │   ├── Commands/    # Command handlers
│   │   ├── Queries/     # Query handlers
│   │   └── DTOs/        # Data transfer objects
├── Common/
│   ├── Interfaces/      # Application service contracts
│   ├── Behaviours/      # MediatR behaviors
│   └── Exceptions/      # Application exceptions
└── Extensions/          # Extension methods

Cleanuperr.Infrastructure/ # External concerns
├── Features/            # Feature-based infrastructure
│   ├── {FeatureName}/
│   │   └── Services/    # External service implementations
├── Http/               # HTTP clients and configurations
├── Logging/            # Logging infrastructure
├── Health/             # Health checks
├── Hubs/               # SignalR hubs
├── Events/             # Event publishing
├── Services/           # Infrastructure services
├── Interceptors/       # Cross-cutting concerns
└── Extensions/         # Infrastructure extensions

Cleanuperr.Persistence/   # Data access layer
├── Context/            # Entity Framework contexts
├── Configurations/     # Entity configurations
├── Repositories/       # Repository implementations
├── Migrations/         # Database migrations
└── Extensions/         # Persistence extensions

Cleanuperr.Api/          # Web API presentation layer
├── Controllers/        # API controllers (thin)
├── Middleware/         # API middleware
├── DependencyInjection/ # DI configuration
├── Jobs/               # Background job scheduling
└── Models/             # API-specific models

Cleanuperr.Shared/       # Cross-cutting concerns
├── Attributes/         # Custom attributes
├── CustomDataTypes/    # Shared data types
├── Helpers/            # Utility helpers
└── Constants/          # Shared constants
```

## File Migration Map

### **Cleanuperr.Domain** (Core Business Logic)

#### Entities (from Data/Models/)
- `Data/Models/Radarr/Movie.cs` → `Cleanuperr.Domain/Entities/Movie.cs`
- `Data/Models/Lidarr/Album.cs` → `Cleanuperr.Domain/Entities/Album.cs`
- `Data/Models/Lidarr/Artist.cs` → `Cleanuperr.Domain/Entities/Artist.cs`
- `Data/Models/Sonarr/Series.cs` → `Cleanuperr.Domain/Entities/Series.cs`
- `Data/Models/Sonarr/Episode.cs` → `Cleanuperr.Domain/Entities/Episode.cs`
- `Data/Models/Arr/Queue/QueueRecord.cs` → `Cleanuperr.Domain/Entities/QueueRecord.cs`
- `Data/Models/Arr/Queue/QueueMovie.cs` → `Cleanuperr.Domain/Entities/QueueMovie.cs`
- `Data/Models/Arr/Queue/QueueAlbum.cs` → `Cleanuperr.Domain/Entities/QueueAlbum.cs`
- `Data/Models/Arr/Queue/QueueSeries.cs` → `Cleanuperr.Domain/Entities/QueueSeries.cs`
- `Data/Models/Arr/Queue/Image.cs` → `Cleanuperr.Domain/Entities/Image.cs`
- `Data/Models/Arr/Queue/LidarrImage.cs` → `Cleanuperr.Domain/Entities/LidarrImage.cs`
- `Data/Models/Events/AppEvent.cs` → `Cleanuperr.Domain/Entities/AppEvent.cs`

#### Enums (from Data/Enums/ and Common/Enums/)
- `Data/Enums/CleanReason.cs` → `Cleanuperr.Domain/Enums/CleanReason.cs`
- `Data/Enums/DeleteReason.cs` → `Cleanuperr.Domain/Enums/DeleteReason.cs`
- `Data/Enums/EventSeverity.cs` → `Cleanuperr.Domain/Enums/EventSeverity.cs`
- `Data/Enums/EventType.cs` → `Cleanuperr.Domain/Enums/EventType.cs`
- `Data/Enums/InstanceType.cs` → `Cleanuperr.Domain/Enums/InstanceType.cs`
- `Data/Enums/StrikeType.cs` → `Cleanuperr.Domain/Enums/StrikeType.cs`
- `Common/Enums/CertificateValidationType.cs` → `Cleanuperr.Domain/Enums/CertificateValidationType.cs`
- `Common/Enums/DownloadClientType.cs` → `Cleanuperr.Domain/Enums/DownloadClientType.cs`
- `Common/Enums/DownloadClientTypeName.cs` → `Cleanuperr.Domain/Enums/DownloadClientTypeName.cs`
- `Data/Models/Configuration/QueueCleaner/BlocklistType.cs` → `Cleanuperr.Domain/Enums/BlocklistType.cs`

#### Value Objects
- `Common/CustomDataTypes/ByteSize.cs` → `Cleanuperr.Domain/ValueObjects/ByteSize.cs`
- `Common/CustomDataTypes/SmartTimeSpan.cs` → `Cleanuperr.Domain/ValueObjects/SmartTimeSpan.cs`

#### Domain Exceptions
- `Common/Exceptions/FatalException.cs` → `Cleanuperr.Domain/Exceptions/FatalException.cs`
- `Data/Models/Deluge/Exceptions/DelugeClientException.cs` → `Cleanuperr.Domain/Exceptions/DelugeClientException.cs`
- `Data/Models/Deluge/Exceptions/DelugeLoginException.cs` → `Cleanuperr.Domain/Exceptions/DelugeLoginException.cs`
- `Data/Models/Deluge/Exceptions/DelugeLogoutException.cs` → `Cleanuperr.Domain/Exceptions/DelugeLogoutException.cs`

### **Cleanuperr.Application** (Use Cases & Application Logic)

#### Features (organize by business capability)

**DownloadCleaner Feature:**
- Create: `Cleanuperr.Application/Features/DownloadCleaner/Commands/CleanDownloadsCommand.cs`
- Create: `Cleanuperr.Application/Features/DownloadCleaner/Commands/CleanDownloadsCommandHandler.cs`
- Create: `Cleanuperr.Application/Features/DownloadCleaner/DTOs/CleanDownloadsResult.cs`

**QueueCleaner Feature:**
- Create: `Cleanuperr.Application/Features/QueueCleaner/Commands/CleanQueueCommand.cs`
- Create: `Cleanuperr.Application/Features/QueueCleaner/Commands/CleanQueueCommandHandler.cs`

**Arr Integration Feature:**
- Create: `Cleanuperr.Application/Features/Arr/Queries/GetQueueQuery.cs`
- Create: `Cleanuperr.Application/Features/Arr/Queries/GetQueueQueryHandler.cs`
- `Executable/DTOs/ArrConfigDto.cs` → `Cleanuperr.Application/Features/Arr/DTOs/ArrConfigDto.cs`
- `Executable/DTOs/CreateArrInstanceDto.cs` → `Cleanuperr.Application/Features/Arr/DTOs/CreateArrInstanceDto.cs`
- `Executable/DTOs/UpdateLidarrConfigDto.cs` → `Cleanuperr.Application/Features/Arr/DTOs/UpdateLidarrConfigDto.cs`
- `Executable/DTOs/UpdateRadarrConfigDto.cs` → `Cleanuperr.Application/Features/Arr/DTOs/UpdateRadarrConfigDto.cs`
- `Executable/DTOs/UpdateSonarrConfigDto.cs` → `Cleanuperr.Application/Features/Arr/DTOs/UpdateSonarrConfigDto.cs`

**DownloadClient Feature:**
- `Executable/DTOs/CreateDownloadClientDto.cs` → `Cleanuperr.Application/Features/DownloadClient/DTOs/CreateDownloadClientDto.cs`

#### Common Application Services
- `Common/Exceptions/ValidationException.cs` → `Cleanuperr.Application/Common/Exceptions/ValidationException.cs`

#### Interfaces (to be implemented in Infrastructure/Persistence)
- Create: `Cleanuperr.Application/Common/Interfaces/IApplicationDbContext.cs`
- Create: `Cleanuperr.Application/Common/Interfaces/IDownloadService.cs`
- Create: `Cleanuperr.Application/Common/Interfaces/IArrClient.cs`
- Create: `Cleanuperr.Application/Common/Interfaces/INotificationService.cs`
- Create: `Cleanuperr.Application/Common/Interfaces/IJobManagementService.cs`

### **Cleanuperr.Infrastructure** (External Concerns)

#### Features (Infrastructure implementations)

**Arr Feature:**
- `Infrastructure/Verticals/Arr/Interfaces/IArrClient.cs` → `Cleanuperr.Infrastructure/Features/Arr/Interfaces/IArrClient.cs`
- `Infrastructure/Verticals/Arr/Interfaces/ILidarrClient.cs` → `Cleanuperr.Infrastructure/Features/Arr/Interfaces/ILidarrClient.cs`
- `Infrastructure/Verticals/Arr/Interfaces/IRadarrClient.cs` → `Cleanuperr.Infrastructure/Features/Arr/Interfaces/IRadarrClient.cs`
- `Infrastructure/Verticals/Arr/Interfaces/ISonarrClient.cs` → `Cleanuperr.Infrastructure/Features/Arr/Interfaces/ISonarrClient.cs`
- `Infrastructure/Verticals/Arr/ArrClient.cs` → `Cleanuperr.Infrastructure/Features/Arr/Services/ArrClient.cs`
- `Infrastructure/Verticals/Arr/ArrClientFactory.cs` → `Cleanuperr.Infrastructure/Features/Arr/Services/ArrClientFactory.cs`
- `Infrastructure/Verticals/Arr/ArrQueueIterator.cs` → `Cleanuperr.Infrastructure/Features/Arr/Services/ArrQueueIterator.cs`
- `Infrastructure/Verticals/Arr/LidarrClient.cs` → `Cleanuperr.Infrastructure/Features/Arr/Services/LidarrClient.cs`
- `Infrastructure/Verticals/Arr/RadarrClient.cs` → `Cleanuperr.Infrastructure/Features/Arr/Services/RadarrClient.cs`
- `Infrastructure/Verticals/Arr/SonarrClient.cs` → `Cleanuperr.Infrastructure/Features/Arr/Services/SonarrClient.cs`

**DownloadCleaner Feature:**
- `Infrastructure/Verticals/DownloadCleaner/DownloadCleaner.cs` → `Cleanuperr.Infrastructure/Features/DownloadCleaner/Services/DownloadCleanerService.cs`

**DownloadClient Feature:**
- `Infrastructure/Verticals/DownloadClient/` → `Cleanuperr.Infrastructure/Features/DownloadClient/`
  - All Deluge, QBittorrent, Transmission services and interfaces
  - `DownloadService.cs`, `DownloadServiceFactory.cs`, `IDownloadService.cs`
  - Result classes: `BlockFilesResult.cs`, `DownloadCheckResult.cs`, `SeedingCheckResult.cs`

**Notifications Feature:**
- `Infrastructure/Verticals/Notifications/` → `Cleanuperr.Infrastructure/Features/Notifications/`
  - All notification providers, consumers, models
  - Apprise and Notifiarr implementations

**ContentBlocker Feature:**
- `Infrastructure/Verticals/ContentBlocker/` → `Cleanuperr.Infrastructure/Features/ContentBlocker/`

**Files Feature:**
- `Infrastructure/Verticals/Files/` → `Cleanuperr.Infrastructure/Features/Files/`

**ItemStriker Feature:**
- `Infrastructure/Verticals/ItemStriker/` → `Cleanuperr.Infrastructure/Features/ItemStriker/`

**Jobs Feature:**
- `Infrastructure/Verticals/Jobs/` → `Cleanuperr.Infrastructure/Features/Jobs/`

**Security Feature:**
- `Infrastructure/Verticals/Security/` → `Cleanuperr.Infrastructure/Features/Security/`

#### Core Infrastructure Services
- `Infrastructure/Health/` → `Cleanuperr.Infrastructure/Health/`
- `Infrastructure/Http/` → `Cleanuperr.Infrastructure/Http/`
- `Infrastructure/Logging/` → `Cleanuperr.Infrastructure/Logging/`
- `Infrastructure/Events/` → `Cleanuperr.Infrastructure/Events/`
- `Infrastructure/Services/` → `Cleanuperr.Infrastructure/Services/`
- `Infrastructure/Interceptors/` → `Cleanuperr.Infrastructure/Interceptors/`
- `Infrastructure/Utilities/` → `Cleanuperr.Infrastructure/Utilities/`
- `Infrastructure/Extensions/` → `Cleanuperr.Infrastructure/Extensions/`
- `Infrastructure/Helpers/` → `Cleanuperr.Infrastructure/Helpers/`
- `Infrastructure/Models/` → `Cleanuperr.Infrastructure/Models/`
- `Infrastructure/Hubs/` → `Cleanuperr.Infrastructure/Hubs/`

### **Cleanuperr.Persistence** (Data Access)

#### Context
- `Data/DataContext.cs` → `Cleanuperr.Persistence/Context/DataContext.cs`
- `Data/EventsContext.cs` → `Cleanuperr.Persistence/Context/EventsContext.cs`

#### Configurations (move EF configurations from entities)
- Create: `Cleanuperr.Persistence/Configurations/MovieConfiguration.cs`
- Create: `Cleanuperr.Persistence/Configurations/AlbumConfiguration.cs`
- Create: `Cleanuperr.Persistence/Configurations/ArtistConfiguration.cs`
- etc. (for each entity)

#### Migrations
- `Data/Migrations/` → `Cleanuperr.Persistence/Migrations/`

#### Converters
- `Data/Converters/LowercaseEnumConverter.cs` → `Cleanuperr.Persistence/Converters/LowercaseEnumConverter.cs`
- `Data/Converters/UtcDateTimeConverter.cs` → `Cleanuperr.Persistence/Converters/UtcDateTimeConverter.cs`

#### Configuration Models (these are DTOs for persistence)
- `Data/Models/Configuration/` → `Cleanuperr.Persistence/Models/Configuration/`
- `Data/Models/Arr/` (non-entity models) → `Cleanuperr.Persistence/Models/Arr/`
- `Data/Models/Deluge/` (non-entity models) → `Cleanuperr.Persistence/Models/External/Deluge/`
- `Data/Models/Cache/` → `Cleanuperr.Persistence/Models/Cache/`

### **Cleanuperr.Api** (Web API Layer)

#### Controllers (convert to thin controllers using MediatR)
- `Executable/Controllers/ApiDocumentationController.cs` → `Cleanuperr.Api/Controllers/ApiDocumentationController.cs`
- `Executable/Controllers/ConfigurationController.cs` → `Cleanuperr.Api/Controllers/ConfigurationController.cs`
- `Executable/Controllers/EventsController.cs` → `Cleanuperr.Api/Controllers/EventsController.cs`
- `Executable/Controllers/HealthCheckController.cs` → `Cleanuperr.Api/Controllers/HealthCheckController.cs`
- `Executable/Controllers/JobsController.cs` → `Cleanuperr.Api/Controllers/JobsController.cs`
- `Executable/Controllers/StatusController.cs` → `Cleanuperr.Api/Controllers/StatusController.cs`

#### DependencyInjection
- `Executable/DependencyInjection/` → `Cleanuperr.Api/DependencyInjection/`

#### Jobs
- `Executable/Jobs/` → `Cleanuperr.Api/Jobs/`

#### Middleware
- `Executable/Middleware/` → `Cleanuperr.Api/Middleware/`

#### Models (API-specific)
- `Executable/Models/` → `Cleanuperr.Api/Models/`

#### Entry Point
- `Executable/Program.cs` → `Cleanuperr.Api/Program.cs`
- `Executable/HostExtensions.cs` → `Cleanuperr.Api/HostExtensions.cs`

### **Cleanuperr.Shared** (Cross-cutting Concerns)

#### Attributes
- `Common/Attributes/SensitiveDataAttribute.cs` → `Cleanuperr.Shared/Attributes/SensitiveDataAttribute.cs`

#### Helpers
- `Common/Helpers/ConfigurationPathProvider.cs` → `Cleanuperr.Shared/Helpers/ConfigurationPathProvider.cs`
- `Common/Helpers/Constants.cs` → `Cleanuperr.Shared/Helpers/Constants.cs`
- `Common/Helpers/StaticConfiguration.cs` → `Cleanuperr.Shared/Helpers/StaticConfiguration.cs`

## Implementation Steps

### Phase 1: Project Setup
1. Delete existing new projects (if any)
2. Create fresh projects with proper dependencies
3. Install required NuGet packages
4. Update solution file

### Phase 2: Domain Layer Migration
1. Create folder structure in Cleanuperr.Domain
2. Move and clean entities (remove EF attributes)
3. Move enums and value objects
4. Move domain exceptions
5. Ensure no external dependencies

### Phase 3: Application Layer Migration
1. Create folder structure in Cleanuperr.Application
2. Create feature-based CQRS structure
3. Move and convert DTOs
4. Define application interfaces
5. Create command/query handlers

### Phase 4: Persistence Layer Migration
1. Create folder structure in Cleanuperr.Persistence
2. Move EF contexts
3. Create entity configurations
4. Move migrations and converters
5. Move persistence-specific models

### Phase 5: Infrastructure Layer Reorganization
1. Reorganize existing Infrastructure by features
2. Move vertical slices to feature-based structure
3. Update dependencies to use Application interfaces
4. Keep external service implementations

### Phase 6: API Layer Migration
1. Move controllers and convert to thin controllers
2. Update controllers to use MediatR
3. Move DI, middleware, jobs
4. Update Program.cs

### Phase 7: Shared Layer Migration
1. Move cross-cutting utilities
2. Move shared attributes and helpers

### Phase 8: Cleanup
1. Update all namespace references
2. Fix using statements
3. Update project references
4. Delete old projects
5. Test compilation and functionality

### Phase 9: Testing & Validation
1. Verify clean architecture principles
2. Test all functionality
3. Update documentation
4. Performance validation

## Success Criteria
- [x] Solution compiles without errors
- [x] All tests pass
- [x] Clean separation of concerns achieved
- [x] Dependency direction flows inward
- [x] Feature-based organization implemented
- [x] CQRS pattern established
- [x] No circular dependencies
- [x] Infrastructure depends only on abstractions

## Files to Delete After Migration
- `Data/` directory (entire)
- `Common/` directory (entire)  
- `Executable/` directory (entire)
- Old solution file references 