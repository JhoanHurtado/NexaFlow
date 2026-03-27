PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
PUBLISH_CMD = dotnet publish "$(PROJECT_DIR)NexaFlow.NexaAuth_Billing.csproj" \
	--configuration Release \
	--framework net10.0 \
	--runtime linux-x64 \
	--self-contained true \
	--output "$(ARTIFACTS_DIR)"

build-RegisterFunction:
	$(PUBLISH_CMD)

build-LoginFunction:
	$(PUBLISH_CMD)

build-CreateUserFunction:
	$(PUBLISH_CMD)

build-ListUsersFunction:
	$(PUBLISH_CMD)

build-DeactivateUserFunction:
	$(PUBLISH_CMD)

build-GetSubscriptionStatusFunction:
	$(PUBLISH_CMD)

build-StripeWebhookFunction:
	$(PUBLISH_CMD)

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
