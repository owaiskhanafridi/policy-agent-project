using PolicyAgent.Models;

namespace PolicyAgent.Repositories
{
    public interface IPolicyRepository
    {
        IReadOnlyList<PolicyChunk> GetAll();
    }
}
