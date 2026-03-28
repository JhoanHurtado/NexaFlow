PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
SRC_DIR     := $(abspath $(PROJECT_DIR)..)
PUBLISH_CMD  = docker run --rm \
	-v "$(SRC_DIR):/src" \
	-v "$(ARTIFACTS_DIR):/out" \
	public.ecr.aws/sam/build-dotnet10 \
	dotnet publish "/src/NexaFlow.NexaPOS/NexaFlow.NexaPOS.csproj" \
		--configuration Release \
		--framework net10.0 \
		--runtime linux-x64 \
		--self-contained true \
		--output /out

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
