PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
PUBLISH_CMD = docker run --rm 	-v "$(PROJECT_DIR)..:/src" 	-v "$(ARTIFACTS_DIR):/out" 	public.ecr.aws/sam/build-dotnet10 	dotnet publish "/src/NexaFlow.NexaBook/NexaFlow.NexaBook.csproj" 		--configuration Release 		--framework net10.0 		--runtime linux-x64 		--self-contained true 		-p:StripSymbols=true 		--output /out

build-NexaFlowNexaBookHandlersCustomerHandlerRegisterCustomerGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersCustomerHandlerUpdateCustomerGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersCustomerHandlerGetCustomerByIdGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersCustomerHandlerListCustomersGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerCreateReservationGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerConfirmReservationGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerCancelReservationGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerMarkArrivedGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerCompleteReservationGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerRescheduleReservationGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerGetReservationByIdGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerListReservationsGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerGetReservationsByCustomerGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerGetAvailabilityGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerGetAgendaGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersReservationHandlerGetSummaryGenerated:
	$(PUBLISH_CMD)
