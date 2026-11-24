<img width="299" height="379" alt="image" src="https://github.com/user-attachments/assets/6da5861e-ae8e-4682-b0a5-860fbca64f6d" />

## Setup

1. Install .NET 10 SDK.
2. Create a MySQL/TiDB database.
3. Copy `appsettings.example.json` to `appsettings.json`.
4. Set the `DefaultConnection` connection string in `appsettings.json`.
5. Restore and run:

```powershell
dotnet restore
dotnet run
```

Do not commit `appsettings.json`, `bin/`, `obj/`, or uploaded payment proof images.

