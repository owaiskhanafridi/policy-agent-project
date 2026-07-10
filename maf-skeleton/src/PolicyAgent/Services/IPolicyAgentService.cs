using PolicyAgent.Models;

namespace PolicyAgent.Services
{
    public interface IPolicyAgentService
    {
        Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken = default);
    }
}
