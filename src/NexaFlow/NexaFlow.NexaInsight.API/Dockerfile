FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NexaFlow.NexaInsight.API/NexaFlow.NexaInsight.API.csproj", "NexaFlow.NexaInsight.API/"]
COPY ["NexaFlow.NexaInsight.Application/NexaFlow.NexaInsight.Application.csproj", "NexaFlow.NexaInsight.Application/"]
COPY ["NexaFlow.NexaInsight.Domain/NexaFlow.NexaInsight.Domain.csproj", "NexaFlow.NexaInsight.Domain/"]
COPY ["NexaFlow.NexaInsight.Infrastructura/NexaFlow.NexaInsight.Infrastructura.csproj", "NexaFlow.NexaInsight.Infrastructura/"]
RUN dotnet restore "NexaFlow.NexaInsight.API/NexaFlow.NexaInsight.API.csproj"
COPY . .
WORKDIR "/src/NexaFlow.NexaInsight.API"
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NexaFlow.NexaInsight.API.dll"]
