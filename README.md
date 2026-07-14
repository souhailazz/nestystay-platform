# NestyStay Platform

Two-folder scaffold for the NestyStay full platform.

## Folders

- `frontend`: Vite React TypeScript web app.
- `backend`: ASP.NET Core .NET 10 API with Domain, Application, Infrastructure, and Api layers.

## Run

Backend:

```powershell
cd backend
dotnet run --project src/NestyStay.Api
```

Frontend:

```powershell
cd frontend
npm run dev
```

## API

Development OpenAPI JSON is exposed by the backend at `/openapi/v1.json`.
