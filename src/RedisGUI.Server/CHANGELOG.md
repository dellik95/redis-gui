This file explains how Visual Studio created the project.

The following steps were used to generate this project:
- Create new ASP\.NET Core Web API project.
- Update `launchSettings.json` to register the SPA proxy as a startup assembly.
- Update project file to add a reference to the frontend project and set SPA properties.
- Add project to the startup projects list.
- Write this file.

Migrations
	Create migration
		- dotnet ef migrations add Initial -p .\RedisGUI.Infrastructure\RedisGUI.Infrastructure.csproj -s .\RedisGUI.Server\RedisGUI.Server.csproj -o  .\Persistence\Migrations
	Remove last migration
		- dotnet ef migrations remove -p .\RedisGUI.Infrastructure\RedisGUI.Infrastructure.csproj -s .\RedisGUI.Server\RedisGUI.Server.csproj
