PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
SRC_DIR     := $(abspath $(PROJECT_DIR)..)
PUBLISH_CMD  = docker run --rm \
	-v "$(SRC_DIR):/src" \
	-v "$(ARTIFACTS_DIR):/out" \
	public.ecr.aws/sam/build-dotnet10 \
	dotnet publish "/src/NexaFlow.NexaInsight/NexaFlow.NexaInsight.csproj" \
		--configuration Release \
		--framework net10.0 \
		--runtime linux-x64 \
		--self-contained true \
		--output /out

build-NexaFlowNexaInsightHandlersInsightHandlerGetAverageTicketGenerated:
	$(PUBLISH_CMD)

build-NexaFlowNexaInsightHandlersInsightHandlerGetCancellationRateGenerated:
	$(PUBLISH_CMD)

build-NexaFlowNexaInsightHandlersInsightHandlerGetDailySummaryGenerated:
	$(PUBLISH_CMD)
