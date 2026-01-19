# ATP Observability Project

Lightweight observability stack and sample app used for SDET test and performance validation.

## Overview

This repository contains a small observability playground with Prometheus, Grafana, Loki, Tempo, and a sample ASP.NET backend plus supporting exporters and a MySQL/MariaDB database. It's designed for local testing, integration checks, and performance tests with k6.

#The app :

<img width="1670" height="472" alt="image" src="https://github.com/user-attachments/assets/2ad932ba-bb31-43f0-a767-2815f0dc77bc" />

---------------------------------

#The app with observability stack:
Distributed Tracing (Tempo)
The system captures the full lifecycle of a request:

Nginx: Captures initial entry and proxy latency.

Backend: Tracks internal .NET logic and middleware.

MySQL: Visualizes exact SQL queries and execution time via MySqlConnector instrumentation.

<img width="1634" height="1184" alt="image" src="https://github.com/user-attachments/assets/3186e200-2f8c-47b7-a0cc-f892aac0cb65" />



## Contents

- `docker-compose.yml` - Main compose file for monitoring stack and exporters.
- `Monitoring.Grafana/` - Grafana data, dashboards, and `prometheus.yml` config.
- `nginx-aspnet-mysql/` - Sample `backend` (ASP.NET), `proxy`, and `db` compose overlay.
- `PerformanceTests.K6/` - k6 performance test scripts.

## Quickstart (local)

Requirements:
- Docker & Docker Compose
- (Optional) `dotnet` SDK to run backend locally

Start the stacks:

```bash for the testing stack
docker compose up -d   
```

```bash for the app stack
cd Monitoring.Grafana
docker compose up -d   
```

```bash for starting k6 stress test 
cd PerformanceTests.K6
k6 run -o experimental-prometheus-rw .\Stress.BreakingPointTest.js   
```

Notes:
- Grafana: http://localhost:3000 (default admin/password)
- Prometheus: http://localhost:9090
- Loki: http://localhost:3100
- Tempo (OTLP): http://localhost:3200
