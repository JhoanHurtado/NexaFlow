using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Handlers
{
    public class ConfigHandler(ITenantConfigService configService)
    {
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/config")]
        public async Task<IHttpResult> GetConfig(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            ILambdaContext context)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            try
            {
                var result = await configService.GetAsync(tenantId);
                return Api.Ok(result);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ConfigHandler.Get] {ex.Message}");
                return Api.InternalServerError("CONFIG_GET_ERROR", "Error al obtener configuración");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Put, "/config")]
        public async Task<IHttpResult> UpdateConfig(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] UpdateTenantConfigRequest body,
            ILambdaContext context)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            try
            {
                var result = await configService.UpdateAsync(tenantId, body);
                return Api.Ok(result);
            }
            catch (DomainException ex)
            {
                return Api.BadRequest("DOMAIN_ERROR", ex.Message);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ConfigHandler.Update] {ex.Message}");
                return Api.InternalServerError("CONFIG_UPDATE_ERROR", "Error al actualizar configuración");
            }
        }
    }
}
