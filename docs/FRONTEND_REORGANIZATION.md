# Frontend Folder Structure Reorganization Plan

## Current Issues:
- Mixed concerns (features, shared components, services scattered)
- No clear separation between feature modules and shared code
- Inconsistent folder naming

## New Structure (Feature-Based + Shared):

```
src/
├── app/
│   ├── core/                          # Core application (singleton services, guards, interceptors)
│   │   ├── guards/
│   │   ├── interceptors/              # HTTP interceptors (moved from shared)
│   │   │   ├── error.interceptor.ts
│   │   │   ├── retry.interceptor.ts
│   │   │   └── loader.interceptor.ts
│   │   ├── models/                    # Core domain models
│   │   │   ├── api-error-response.model.ts
│   │   │   └── index.ts
│   │   ├── services/                  # Core singleton services
│   │   │   ├── loading.service.ts
│   │   │   └── section-actions-portal.service.ts
│   │   └── index.ts
│   │
│   ├── shared/                        # Shared/reusable components, directives, pipes
│   │   ├── components/                # Dumb/presentational components
│   │   │   ├── json-editor/
│   │   │   ├── loading/
│   │   │   ├── notifications/
│   │   │   ├── toggler/
│   │   │   └── tree-view-list/
│   │   ├── directives/
│   │   ├── pipes/
│   │   └── index.ts
│   │
│   ├── features/                      # Feature modules (Smart components + feature-specific logic)
│   │   ├── dashboard/                 # Dashboard feature
│   │   │   ├── components/
│   │   │   │   ├── dashboard-section-actions/
│   │   │   │   ├── edit-connection/
│   │   │   │   └── storage-connections-list/
│   │   │   ├── services/
│   │   │   │   └── edit-connection-dialog.service.ts
│   │   │   ├── models/
│   │   │   │   └── connection-dialog-data.type.ts
│   │   │   ├── dashboard.component.ts
│   │   │   ├── dashboard.component.html
│   │   │   ├── dashboard.component.scss
│   │   │   └── index.ts
│   │   │
│   │   ├── storage-management/        # Redis storage feature
│   │   │   ├── components/
│   │   │   │   ├── storage-values-list/
│   │   │   │   ├── value-editor-dialog/
│   │   │   │   ├── inline-value-editor/
│   │   │   │   ├── bulk-actions-toolbar/
│   │   │   │   └── storage-resources-info/
│   │   │   ├── services/
│   │   │   │   └── storage-values.datasource.ts
│   │   │   ├── models/
│   │   │   ├── storage-management.component.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── redis-operations/          # Advanced Redis operations
│   │   │   ├── transaction-builder/
│   │   │   ├── lua-script-console/
│   │   │   └── pub-sub-panel/
│   │   │
│   │   ├── health-monitoring/
│   │   │   └── connection-health-card/
│   │   │
│   │   └── info/
│   │
│   ├── services/                      # Application-wide services (API services)
│   │   ├── api/                       # API communication services
│   │   │   ├── redis-keys.service.ts
│   │   │   ├── redis-operations.service.ts
│   │   │   ├── storage-connections.service.ts
│   │   │   └── index.ts
│   │   └── index.ts
│   │
│   ├── models/                        # Application-wide models/types
│   │   ├── connection.type.ts
│   │   ├── redis-keys.types.ts
│   │   ├── redis-advanced.types.ts
│   │   └── index.ts
│   │
│   ├── app.component.ts
│   ├── app.config.ts
│   └── app.routes.ts
│
├── assets/
├── styles/
└── index.html
```

## Migration Strategy:

### Phase 1: Core Setup
1. Create `/app/core` folder structure
2. Move interceptors from `/shared/interceptors` to `/app/core/interceptors`
3. Move core services (LoadingService, SectionActionsPortalService) to `/app/core/services`
4. Create `/app/core/models` for domain models

### Phase 2: Shared Components
1. Keep `/shared/components` but only for reusable presentational components
2. Move feature-specific components to feature folders

### Phase 3: Features
1. Create `/app/features` folder
2. Reorganize dashboard → `/app/features/dashboard`
3. Reorganize storage-management → `/app/features/storage-management`
4. Move transaction, lua, pubsub → `/app/features/redis-operations`

### Phase 4: Services & Models
1. Create `/app/services/api` for API services
2. Create `/app/models` for shared types
3. Move services from `/shared/services` to `/app/services/api`
4. Move types from `/shared/types` to `/app/models`

### Phase 5: Cleanup
1. Remove empty folders
2. Update all imports
3. Create barrel exports (index.ts files)

## Benefits:
- ✅ Clear separation of concerns
- ✅ Feature-based organization (easier to find code)
- ✅ Scalable structure for future features
- ✅ Better encapsulation
- ✅ Easier testing
- ✅ Follows Angular best practices

## Implementation Notes:
- Use barrel exports (index.ts) for cleaner imports
- Keep shared components truly reusable
- Feature folders should be self-contained
- Core should only contain singletons
