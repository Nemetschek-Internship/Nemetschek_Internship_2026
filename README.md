# Nemetschek_Internship_2026

Shkolo 2.0

## Branch Onboarding: Run And Build With Docker

This project runs with Docker Compose from the NemeBook folder.

### 1. Prerequisites

- Docker Desktop installed and running
- Docker Compose available (included in modern Docker Desktop)
- Port 1433 available on your machine
- Port 8081 available on your machine (default host port for the web app)

### 2. First-Time Setup

1. Open a terminal in the repository root.
2. Go to the compose folder:

```powershell
cd NemeBook
```

3. Confirm the environment file exists:

```powershell
Get-ChildItem .env
```

4. If needed, edit .env values before first run:
- MSSQL_SA_PASSWORD
- WEB_HOST_PORT (default: 8081)
- ASPNETCORE_ENVIRONMENT

### 3. Build And Start Containers

Run this from NemeBook:

```powershell
docker compose up --build -d
```

This starts:
- mssql on host port 1433
- web on host port WEB_HOST_PORT (default 8081)

### 4. Verify Everything Is Running

Check service status:

```powershell
docker compose ps
```

Verify web is reachable:

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:8081 | Select-Object StatusCode,StatusDescription
```

Expected result: 200 OK

### 5. Rebuild After Code Changes

```powershell
docker compose up --build -d
```

### 6. View Logs

Web logs:

```powershell
docker compose logs web --tail 100
```

SQL logs:

```powershell
docker compose logs mssql --tail 100
```

### 7. Stop Containers

```powershell
docker compose down
```

### 8. Reset Database Volume (Destructive)

Use this only when you need a clean SQL state:

```powershell
docker compose down -v
```

### Troubleshooting

- If web fails to start with a port bind error, change WEB_HOST_PORT in .env (for example 8082), then run docker compose up --build -d again.
- If SQL is slow on first boot, wait until mssql is healthy in docker compose ps.
- If startup fails after pulling latest changes, run docker compose down, then docker compose up --build -d.
