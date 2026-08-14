# syntax=docker/dockerfile:1

# --- Stage 1: build & publish -----------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy chỉ các .csproj trước để tận dụng cache layer khi restore - source code
# thay đổi không làm invalidate bước restore (chậm nhất) trừ khi thêm/bớt package.
COPY src/TaskMgmt.Domain/*.csproj src/TaskMgmt.Domain/
COPY src/TaskMgmt.Application/*.csproj src/TaskMgmt.Application/
COPY src/TaskMgmt.Infrastructure/*.csproj src/TaskMgmt.Infrastructure/
COPY src/TaskMgmt.API/*.csproj src/TaskMgmt.API/
RUN dotnet restore src/TaskMgmt.API/TaskMgmt.API.csproj

COPY src/ src/
RUN dotnet publish src/TaskMgmt.API/TaskMgmt.API.csproj \
    -c Release \
    -o /app/publish

# --- Stage 2: runtime ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl chỉ dùng cho HEALTHCHECK bên dưới (base image không có sẵn).
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Chạy bằng user thường (có sẵn trong base image), không chạy container bằng root.
USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TaskMgmt.API.dll"]
