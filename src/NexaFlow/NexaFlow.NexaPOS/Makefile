PUBLISH_CMD = dotnet publish "$(CURDIR)/NexaFlow.NexaPOS.csproj" \
	--configuration Release \
	--framework net10.0 \
	--runtime linux-x64 \
	--self-contained true \
	--output "$(ARTIFACTS_DIR)"

build-NexaFlowNexaPOSHandlersCustomerHandlerCreateCustomerGenerated:
	$(PUBLISH_CMD)

build-NexaFlowNexaPOSHandlersCustomerHandlerListCustomersGenerated:
	$(PUBLISH_CMD)

build-NexaFlowNexaPOSHandlersSaleHandlerCreateSaleGenerated:
	$(PUBLISH_CMD)

build-NexaFlowNexaPOSHandlersSaleHandlerGetSaleByIdGenerated:
	$(PUBLISH_CMD)

build-NexaFlowNexaPOSHandlersSaleHandlerListSalesGenerated:
	$(PUBLISH_CMD)

build-NexaFlowNexaPOSHandlersProductHandlerCreateGenerated:
	$(PUBLISH_CMD)

build-NexaFlowNexaPOSHandlersProductHandlerListGenerated:
	$(PUBLISH_CMD)
