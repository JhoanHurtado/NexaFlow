PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
# Framework-dependent build: no AOT, no self-contained, no GLIBC dependency.
# The Lambda dotnet10 runtime provides the .NET runtime on AL2023.
PUBLISH_CMD = dotnet publish "$(PROJECT_DIR)NexaFlow.NexaPOS.csproj" \
	--configuration Release \
	--framework net10.0 \
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
