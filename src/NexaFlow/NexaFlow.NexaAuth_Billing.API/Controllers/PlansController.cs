using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;

namespace NexaFlow.NexaAuth_Billing.API.Controllers;

[ApiController]
[Route("plans")]
[Produces("application/json")]
public class PlansController(IPlanRepository planRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPlans()
    {
        try
        {
            var plans = await planRepository.GetAllAsync();
            var data = plans.Select(p => new
            {
                Id            = p.Id,
                Name          = p.Name,
                Price         = p.Price,
                MaxUsers      = p.MaxUsers,
                StripePriceId = p.StripePriceId,
            });
            return Ok(ApiResponse<object>.Ok(data));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.Fail("PLANS_ERROR", "Error al obtener planes"));
        }
    }
}
