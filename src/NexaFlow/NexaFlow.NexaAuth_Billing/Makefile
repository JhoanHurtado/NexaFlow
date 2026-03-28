PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
# Framework-dependent build: no AOT, no self-contained, no GLIBC dependency.
# The Lambda dotnet10 runtime provides the .NET runtime on AL2023.
PUBLISH_CMD = dotnet publish "$(PROJECT_DIR)NexaFlow.NexaAuth_Billing.csproj" \
	--configuration Release \
	--framework net10.0 \
	--output "$(ARTIFACTS_DIR)"

build-NexaFlowNexaAuthBillingHandlersAuthHandlerRegisterGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaAuthBillingHandlersAuthHandlerLoginGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaAuthBillingHandlersSubscriptionHandlerGetStatusGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaAuthBillingHandlersSubscriptionHandlerStripeWebhookGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaAuthBillingHandlersUserHandlerCreateUserGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaAuthBillingHandlersUserHandlerListUsersGenerated:
	$(PUBLISH_CMD)
build-NexaFlowNexaAuthBillingHandlersUserHandlerDeactivateUserGenerated:
	$(PUBLISH_CMD)
