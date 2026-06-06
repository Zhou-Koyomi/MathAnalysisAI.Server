# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MathAnalysisAI.Server.csproj ./
RUN dotnet restore ./MathAnalysisAI.Server.csproj

COPY . .
RUN dotnet publish ./MathAnalysisAI.Server.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 5131
ENTRYPOINT ["dotnet", "MathAnalysisAI.Server.dll"]
