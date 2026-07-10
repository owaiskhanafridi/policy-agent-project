using PolicyAgent.Models;

namespace PolicyAgent.Services
{
    public interface IPolicySearchService
    {
        Task<IReadOnlyList<PolicySearchResult>> SearchAsync(string question, CancellationToken cancellationToken = default);
    }
}
