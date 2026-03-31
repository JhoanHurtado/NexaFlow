PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
# Framework-dependent build: no AOT, no self-contained, no GLIBC dependency.
# The Lambda dotnet10 runtime provides the .NET runtime on AL2023.
PUBLISH_CMD = dotnet publish "$(PROJECT_DIR)NexaFlow.NexaBook.csproj" \
	--configuration Release \
	--framework net10.0 \
	--output "$(ARTIFACTS_DIR)"

build-NexaFlowNexaBookHandlersCustomerHandlerRegisterCustomerGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaBookHandlersCustomerHandlerFindOrCreateCustomerGenerated:
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
