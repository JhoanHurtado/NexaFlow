PROJECT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
PUBLISH_CMD = docker run --rm 	-v "$(PROJECT_DIR)..:/src" 	-v "$(ARTIFACTS_DIR):/out" 	public.ecr.aws/sam/build-dotnet10 	dotnet publish "/src/NexaFlow.NexaAuth_Billing/NexaFlow.NexaAuth_Billing.csproj" 		--configuration Release 		--framework net10.0 		--runtime linux-x64 		--self-contained true 		-p:StripSymbols=true 		--output /out

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
