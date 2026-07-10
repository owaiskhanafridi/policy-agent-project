using Microsoft.AspNetCore.Mvc;
using PolicyAgent.Models;
using PolicyAgent.Services;

namespace PolicyAgent.Controllers
{
    [ApiController]
    [Route("api/evaluation")]
    public class EvaluationController(EvaluationService evaluationService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> RunEvaluation()
        {
            var results = await evaluationService.RunAsync();
            return Ok(results);
        }
    }
}
