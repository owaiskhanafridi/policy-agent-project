using Microsoft.Extensions.AI;
using PolicyAgent.Models;
using PolicyAgent.Repositories;
using System.Numerics.Tensors;

namespace PolicyAgent.Services
{
    public class EmbeddingPolicySearchService : IPolicySearchService
    {
        private const int MaxResults = 3; // TODO: test it later with other values

        private readonly IPolicyRepository _policyRepository;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly SemaphoreSlim _initializeLock = new(1, 1);

        private List<EmbeddedPolicyChunk>? _embeddedChunks;

        public EmbeddingPolicySearchService(
            IPolicyRepository policyRepository,
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
        {
            _policyRepository = policyRepository;
            _embeddingGenerator = embeddingGenerator;
        }

        public async Task<IReadOnlyList<PolicySearchResult>> SearchAsync(
            string question,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return [];
            }

            await EnsureInitializedAsync(cancellationToken);

            var questionEmbedding = await GenerateEmbeddingAsync(
                question,
                cancellationToken);

            var result = _embeddedChunks!
                .Select(item => new PolicySearchResult(
                    Chunk: item.Chunk,
                    Score: TensorPrimitives.CosineSimilarity(questionEmbedding, item.Embedding)))
                .OrderByDescending(result => result.Score)
                .Take(MaxResults)
                .ToList();

            foreach (var r in result)
            {
                Console.WriteLine($"{r.Score:F3} - {r.Chunk.Citation} - {r.Chunk.Text[..Math.Min(120, r.Chunk.Text.Length)]}");
            }

            return result;
        }

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_embeddedChunks is not null)
            {
                return;
            }

            await _initializeLock.WaitAsync(cancellationToken);

            try
            {
                if (_embeddedChunks is not null)
                {
                    return;
                }

                var chunks = _policyRepository.GetAll();

                var embeddedChunks = new List<EmbeddedPolicyChunk>();

                foreach (var chunk in chunks)
                {
                    var embedding = await GenerateEmbeddingAsync(
                        ToSearchText(chunk),
                        cancellationToken);

                    embeddedChunks.Add(new EmbeddedPolicyChunk(chunk, embedding));
                }

                _embeddedChunks = embeddedChunks;
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        private async Task<float[]> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken)
        {
            var embedding = await _embeddingGenerator.GenerateAsync(
                text,
                cancellationToken: cancellationToken);

            return embedding.Vector.ToArray();
        }

        private static string ToSearchText(PolicyChunk chunk)
        {
            return $"""
                Document: {chunk.DocumentId}
                Section: {chunk.SectionId}
                Citation: {chunk.Citation}

                {chunk.Text}
             """;
        }
    }
}
