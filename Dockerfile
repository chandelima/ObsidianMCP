FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ObsidianMCP.slnx .
COPY ObsidianMCP.Domain/ObsidianMCP.Domain.csproj ObsidianMCP.Domain/
COPY ObsidianMCP.Application/ObsidianMCP.Application.csproj ObsidianMCP.Application/
COPY ObsidianMCP.Infrastructure/ObsidianMCP.Infrastructure.csproj ObsidianMCP.Infrastructure/
COPY ObsidianMCP.API/ObsidianMCP.API.csproj ObsidianMCP.API/
RUN dotnet restore ObsidianMCP.API/ObsidianMCP.API.csproj

COPY . .
RUN dotnet publish ObsidianMCP.API/ObsidianMCP.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ObsidianMCP.API.dll"]
