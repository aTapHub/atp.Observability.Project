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

## Testing process and observability conclusions

First, we run the k6 stress script:


<img width="2300" height="800" alt="image" src="https://github.com/user-attachments/assets/fc83c513-0b1f-4d35-b8c6-166bebf12a39" />


Afterwards, we open Grafana and use the 4 Golden Signals tier 0 dahsboard. This is used as a first line of check-ups to see if any of the 4 golden signals (Latency, Traffic, Errors, and Saturation)
We also keep an eye out if any threshholds defined in the k6 script failed.

The 4 golden signals:

<img width="2194" height="1166" alt="image" src="https://github.com/user-attachments/assets/8d33cc76-02c2-4931-8078-ed2f3132ffbb" />

Threshholds exceeded for k6 script ()

<img width="2128" height="1134" alt="image" src="https://github.com/user-attachments/assets/96365679-348a-4ef2-9988-cb3babb1a442" />


- Performance Degradation: The system hit a "breakpoint" at 500 VUs. While the application did not crash (0% errors), response times tripled, indicating significant queuing.
 - Resource Contention: CPU hit 60% and Memory usage plateaued at a higher baseline. The gap between $p95$ and $p99$ suggests the bottleneck is likely Database I/O locking or connection pool exhaustion rather than raw CPU limits.
- "Fail-Slow" Behavior: The application prioritizes request completion over rejection, leading to high latency spikes during traffic bursts.


