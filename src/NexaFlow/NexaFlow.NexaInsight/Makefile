build-GetAverageTicketFunction:
build-GetCancellationRateFunction:
build-GetDailySummaryFunction:
build-NexaFlowNexaInsightHandlersInsightHandlerGetAverageTicketGenerated:
build-NexaFlowNexaInsightHandlersInsightHandlerGetCancellationRateGenerated:
build-NexaFlowNexaInsightHandlersInsightHandlerGetDailySummaryGenerated:
	dotnet publish $(MAKE_ROOT)/NexaFlow.NexaInsight.csproj \
		--configuration Release \
		--framework net10.0 \
		--runtime linux-x64 \
		--self-contained true \
		--output $(ARTIFACTS_DIR)
