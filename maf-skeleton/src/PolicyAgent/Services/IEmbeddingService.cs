namespace PolicyAgent.Services
{
    public interface IEmbeddingService
    {
        Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default);
    }
}
