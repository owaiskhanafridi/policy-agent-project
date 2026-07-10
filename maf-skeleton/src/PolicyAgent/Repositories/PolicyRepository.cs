using PolicyAgent.Models;
using System.Text.RegularExpressions;

namespace PolicyAgent.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly List<PolicyChunk> _chunks = [];

    public PolicyRepository(IWebHostEnvironment environment)
    {
        var policiesPath = Path.Combine(environment.ContentRootPath, "..", "..", "..", "policies");

        if (!Directory.Exists(policiesPath))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(policiesPath, "*.md"))
        {
            var documentId = GetDocumentId(file);
            var text = File.ReadAllText(file);

            var chunks = ParsePolicy(documentId, text);
            _chunks.AddRange(chunks);
        }
    }

    public IReadOnlyList<PolicyChunk> GetAll() => _chunks;

    private static string GetDocumentId(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        if (fileName.Contains("acceptable-use", StringComparison.OrdinalIgnoreCase))
            return "AUP";

        if (fileName.Contains("vendor-management", StringComparison.OrdinalIgnoreCase))
            return "VMP";

        if (fileName.Contains("data-classification", StringComparison.OrdinalIgnoreCase))
            return "DCS-2025";

        if (fileName.Contains("travel-and-expense", StringComparison.OrdinalIgnoreCase))
            return "TE-2025";

        return fileName;
    }

    private static List<PolicyChunk> ParsePolicy(string documentId, string text)
    {
        var chunks = new List<PolicyChunk>();

        var lines = text.Split('\n', StringSplitOptions.None);

        string? currentSection = null;
        var buffer = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            var sectionMatch = Regex.Match(
                line,
                @"^(?:#{1,6}\s*)?§?\s*(\d+(?:\.\d+)*)(?:\.)?\s+(.+)$");

            if (sectionMatch.Success)
            {
                if (currentSection is not null && buffer.Count > 0)
                {
                    chunks.Add(CreateChunk(documentId, currentSection, buffer));
                    buffer.Clear();
                }

                currentSection = sectionMatch.Groups[1].Value;
                buffer.Add(line);
                continue;
            }

            if (currentSection is not null)
            {
                buffer.Add(line);
            }
        }

        if (currentSection is not null && buffer.Count > 0)
        {
            chunks.Add(CreateChunk(documentId, currentSection, buffer));
        }

        return chunks;
    }

    private static PolicyChunk CreateChunk(string documentId, string sectionId, List<string> lines)
    {
        var text = string.Join(Environment.NewLine, lines).Trim();

        return new PolicyChunk(
            DocumentId: documentId,
            SectionId: sectionId,
            Citation: $"{documentId} §{sectionId}",
            Text: text);
    }
}