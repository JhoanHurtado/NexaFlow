PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
PUBLISH_CMD = docker run --rm \
	-v "$(PROJECT_DIR)..:/src" \
	-v "$(ARTIFACTS_DIR):/out" \
	public.ecr.aws/sam/build-dotnet10 \
	dotnet publish "/src/NexaFlow.NexaInsight/NexaFlow.NexaInsight.csproj" \
		--configuration Release \
		--framework net10.0 \
		--runtime linux-x64 \
		--self-contained true \
		--output /out

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
