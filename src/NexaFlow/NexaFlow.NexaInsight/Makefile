PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
PUBLISH_CMD = dotnet publish "$(PROJECT_DIR)NexaFlow.NexaInsight.csproj" \
	--configuration Release \
	--framework net10.0 \
	--runtime linux-x64 \
	--self-contained true \
	--output "$(ARTIFACTS_DIR)"

build-GetAverageTicketFunction:
	$(PUBLISH_CMD)

build-GetCancellationRateFunction:
	$(PUBLISH_CMD)

build-GetDailySummaryFunction:
	$(PUBLISH_CMD)

build-NexaFlowNexaInsightHandlersInsightHandlerGetAverageTicketGenerated:
	$(PUBLISH_CMD)

build-NexaFlowNexaInsightHandlersInsightHandlerGetCancellationRateGenerated:
	$(PUBLISH_CMD)

build-NexaFlowNexaInsightHandlersInsightHandlerGetDailySummaryGenerated:
	$(PUBLISH_CMD)
