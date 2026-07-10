using PolicyAgent.Models;

namespace PolicyAgent.Services
{
    public class PolicySearchTool(IPolicySearchService policySearchService)
    {
        public async Task<IReadOnlyList<PolicyEvidence>> SearchAsync(
            string question,
            CancellationToken cancellationToken = default)
        {
            var results = await policySearchService.SearchAsync(
                question,
                cancellationToken);

            return results
                .Select(result => new PolicyEvidence(
                    Citation: result.Chunk.Citation,
                    Text: result.Chunk.Text,
                    Score: result.Score))
                .ToList();
        }
    }

}
