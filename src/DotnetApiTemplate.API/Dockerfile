FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080

RUN apk add --no-cache icu-libs icu-data-full tzdata curl
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV TZ=Europe/Paris

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS publish
WORKDIR /src

COPY ["src/DotnetApiTemplate.API/DotnetApiTemplate.API.csproj", "src/DotnetApiTemplate.API/"]
COPY ["src/DotnetApiTemplate.Domain/DotnetApiTemplate.Domain.csproj", "src/DotnetApiTemplate.Domain/"]
COPY ["src/DotnetApiTemplate.Application/DotnetApiTemplate.Application.csproj", "src/DotnetApiTemplate.Application/"]
COPY ["src/DotnetApiTemplate.Persistence/DotnetApiTemplate.Persistence.csproj", "src/DotnetApiTemplate.Persistence/"]
COPY ["src/DotnetApiTemplate.Infrastructure/DotnetApiTemplate.Infrastructure.csproj", "src/DotnetApiTemplate.Infrastructure/"]
RUN dotnet restore "src/DotnetApiTemplate.API/DotnetApiTemplate.API.csproj"

COPY . .
WORKDIR "/src/src/DotnetApiTemplate.API"
RUN dotnet publish "DotnetApiTemplate.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotnetApiTemplate.API.dll"]

HEALTHCHECK CMD curl --fail http://localhost:8080/health || exit
