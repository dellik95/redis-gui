# RedisGUI — Solution Structure

RedisGUI keeps the domain, application use cases, infrastructure, API, and Angular client in separate projects. `RedisGUI.slnx` also contains dedicated test projects.

```text
src/
  RedisGUI.Shared/           Result types and in-process mediator
  RedisGUI.Domain/           Domain models and contracts
  RedisGUI.Application/      Commands, queries, validators, and pipeline behaviors
  RedisGUI.Infrastructure/   Redis, persistence, authentication, and SignalR adapters
  RedisGUI.Server/           HTTP endpoints, middleware, and application composition
  RedisGUI.Client/           Angular application

test/
  RedisGUI.Shared.Tests/
  RedisGUI.Domain.Tests/
  RedisGUI.Application.Tests/
  RedisGUI.Infrastructure.Tests/
  RedisGUI.Server.Tests/     HTTP responses and middleware
```

## Application features

Redis use cases are grouped by feature in `Redis/Keys`, `Redis/DataTransfer`, `Redis/Diagnostics`, `Redis/Messaging`, and `Redis/Tools`. Connection, user, audit, and metrics use cases have their own folders. Each command or query shares its file with its handler; feature-specific validators and response models live beside it. Existing namespaces are retained during the folder reorganization so consumers do not need to change imports.

## Infrastructure and API

Redis infrastructure services are grouped under `Redis/Services` by responsibility: `Connections`, `Keys`, `Messaging`, `Diagnostics`, and `Tools`. HTTP endpoint files are grouped under `Endpoints/Connections`, `Keys`, `Messaging`, `DataTransfer`, `Diagnostics`, `Tools`, and `Audit`. Endpoint routes are unchanged by the folder organization.

The messaging endpoints dispatch requests through the application mediator. Long-running operations use the background job queue: HTTP starts, cancels, checks, and retrieves jobs; the authenticated jobs hub publishes status and progress to the owning user. The worker runs application handlers in an isolated scope with the initiating identity, and checks connection access again. Clients resubscribe and retrieve current status after a SignalR reconnect.

The current queue and results are held in the application process and expire 30 minutes after completion. File results use temporary server storage and are removed at expiration or graceful shutdown; an unclean shutdown can leave files behind. Jobs do not survive a restart and are not shared between application instances; multi-instance deployments need shared job storage and coordinated processing before relying on this implementation. The queue is bounded and rejects new work when full. Load-test jobs require write access and Safe Mode to be off.

## Validation and pipeline

The application registers FluentValidation request validators from its assembly. The in-process mediator resolves open pipeline behaviors for the concrete request type, allowing request-specific validators to run before the handler. Explicitly registered interface-level behaviors remain supported. Validation failures are returned as `Validation.Failed` with field-level messages and are mapped to HTTP validation Problem Details.

Domain invariants, access checks, connection state, and Redis protocol checks remain at their owning boundaries because they must also protect non-HTTP callers and changing external state. FluentValidation handles request input rules; it does not replace those invariants.

## Client

Angular components and services specific to storage management live under `features/storage-management`; application-wide connection and key state remain in `core`. Shared controls remain under `shared`.

## Running tests

Run `dotnet test RedisGUI.slnx`. Redis and MySQL integration tests require their documented environment variables; tests without the required service configuration are skipped. Run the client build with `npm run build` from `src/RedisGUI.Client`.

The opt-in cluster failover test changes Redis topology. Run it only against a disposable cluster with `REDISGUI_TEST_CLUSTER_FAILOVER` set to the connection string (including `allowAdmin=true`) and `REDISGUI_TEST_CLUSTER_FAILOVER_PRIMARY` and `REDISGUI_TEST_CLUSTER_FAILOVER_REPLICA` set to the corresponding `host:port` endpoints.
