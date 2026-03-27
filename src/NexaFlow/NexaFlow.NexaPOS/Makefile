build-NexaFlowNexaPOSHandlersCustomerHandlerCreateCustomerGenerated:
build-NexaFlowNexaPOSHandlersCustomerHandlerListCustomersGenerated:
build-NexaFlowNexaPOSHandlersSaleHandlerCreateSaleGenerated:
build-NexaFlowNexaPOSHandlersSaleHandlerGetSaleByIdGenerated:
build-NexaFlowNexaPOSHandlersSaleHandlerListSalesGenerated:
build-NexaFlowNexaPOSHandlersProductHandlerCreateGenerated:
build-NexaFlowNexaPOSHandlersProductHandlerListGenerated:
	dotnet publish $(MAKE_ROOT)/NexaFlow.NexaPOS.csproj \
		--configuration Release \
		--framework net10.0 \
		--runtime linux-x64 \
		--self-contained true \
		--output $(ARTIFACTS_DIR)
