using Microsoft.AspNetCore.Mvc;
using PolicyAgent.Models;
using PolicyAgent.Services;

namespace PolicyAgent.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class PolicyAgentController(IPolicyAgentService _policyAgentService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Chat(ChatRequest request)
        {

            var response = await _policyAgentService.AskAsync(request);
            return Ok(response);
        }
    }
}
