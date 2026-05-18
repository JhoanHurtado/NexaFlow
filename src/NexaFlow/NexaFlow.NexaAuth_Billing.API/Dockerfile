FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NexaFlow.NexaAuth_Billing.API/NexaFlow.NexaAuth_Billing.API.csproj", "NexaFlow.NexaAuth_Billing.API/"]
COPY ["NexaFlow.NexaAuth_Billing.Application/NexaFlow.NexaAuth_Billing.Application.csproj", "NexaFlow.NexaAuth_Billing.Application/"]
COPY ["NexaFlow.NexaAuth_Billing.Domain/NexaFlow.NexaAuth_Billing.Domain.csproj", "NexaFlow.NexaAuth_Billing.Domain/"]
COPY ["NexaFlow.NexaAuth_Billing.Infrastructure/NexaFlow.NexaAuth_Billing.Infrastructure.csproj", "NexaFlow.NexaAuth_Billing.Infrastructure/"]
RUN dotnet restore "NexaFlow.NexaAuth_Billing.API/NexaFlow.NexaAuth_Billing.API.csproj"
COPY . .
WORKDIR "/src/NexaFlow.NexaAuth_Billing.API"
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NexaFlow.NexaAuth_Billing.API.dll"]
