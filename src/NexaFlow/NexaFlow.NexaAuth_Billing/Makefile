build-RegisterFunction:
build-LoginFunction:
build-CreateUserFunction:
build-ListUsersFunction:
build-DeactivateUserFunction:
build-GetSubscriptionStatusFunction:
build-StripeWebhookFunction:
build-NexaFlowNexaAuthBillingHandlersAuthHandlerRegisterGenerated:
build-NexaFlowNexaAuthBillingHandlersAuthHandlerLoginGenerated:
build-NexaFlowNexaAuthBillingHandlersSubscriptionHandlerGetStatusGenerated:
build-NexaFlowNexaAuthBillingHandlersSubscriptionHandlerStripeWebhookGenerated:
build-NexaFlowNexaAuthBillingHandlersUserHandlerCreateUserGenerated:
build-NexaFlowNexaAuthBillingHandlersUserHandlerListUsersGenerated:
build-NexaFlowNexaAuthBillingHandlersUserHandlerDeactivateUserGenerated:
	dotnet publish $(MAKE_ROOT)/NexaFlow.NexaAuth_Billing.csproj \
		--configuration Release \
		--framework net10.0 \
		--runtime linux-x64 \
		--self-contained true \
		--output $(ARTIFACTS_DIR)
