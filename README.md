# ApiDemo – File-backed .NET Web API

This repository contains a .NET 9 Web API that exposes CRUD operations for a simple `Product` resource. Data is persisted to a CSV file on disk, giving you a lightweight, file-based storage option without any external dependencies.

## Features
- RESTful controller with endpoints under `/api/products`
- CSV-backed repository with basic concurrency safety and CSV escaping
- OpenAPI/Swagger support for interactive exploration
- Dockerfile for containerized deployments
- GitHub Actions workflow for restore, build, test, and Docker image validation

## Prerequisites
- .NET SDK 9.0
- Docker (optional, required for container builds)

## Getting Started
```bash
# Restore dependencies
dotnet restore

# Run the API (HTTPS disabled for simplicity here)
dotnet run --project ApiDemo.Api
```

The API listens on `https://localhost:7183` and `http://localhost:5285` by default (as defined by the generated `launchSettings.json`). Swagger UI is available at `/swagger` when running in development.

## CSV Storage
- Default location: `ApiDemo.Api/Data/products.csv`
- The path can be overridden via `appsettings.json` (`CsvStorage:ProductsFile`)
- The repository automatically creates the CSV file and header when missing

## Docker
```bash
# Build the container
docker build -t apidemo-api .

# Run the container on port 8080
docker run --rm -p 8080:8080 apidemo-api
```

## Continuous Integration
The workflow defined in `.github/workflows/dotnet-ci.yml` runs on pushes/pull requests to `main` and performs:
1. `dotnet restore`
2. `dotnet build --configuration Release`
3. `dotnet test` (placeholder—add tests to enable coverage)
4. `docker build` to ensure the container image builds successfully

## Next Steps
- Add automated tests (unit and/or integration) under a new test project
- Extend the API with pagination or search if required
