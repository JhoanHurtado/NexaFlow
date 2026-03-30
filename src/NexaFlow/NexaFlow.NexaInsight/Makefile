PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
# Framework-dependent build: no AOT, no self-contained, no GLIBC dependency.
# The Lambda dotnet10 runtime provides the .NET runtime on AL2023.
PUBLISH_CMD = dotnet publish "$(PROJECT_DIR)NexaFlow.NexaInsight.csproj" \
	--configuration Release \
	--framework net10.0 \
	--output "$(ARTIFACTS_DIR)"

build-NexaFlowNexaInsightHandlersInsightHandlerGetAverageTicketGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaInsightHandlersInsightHandlerGetCancellationRateGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaInsightHandlersInsightHandlerGetDailySummaryGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaInsightHandlersInsightHandlerGetTopProductsGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaInsightHandlersInsightHandlerGetLowStockProductsGenerated:
	$(PUBLISH_CMD)
