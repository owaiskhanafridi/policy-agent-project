using Microsoft.Agents.AI;
using PolicyAgent.Models;

namespace PolicyAgent.Services;

public class PolicyAgentService(AIAgent agent, ConversationMemoryService memory) : IPolicyAgentService
{
    public async Task<ChatResponse> AskAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return new ChatResponse(
                "Please provide a policy question.",
                [],
                false);
        }

        var recentMemory = memory.GetRecentConversations();

        var memoryText = string.Join(
            Environment.NewLine,
            recentMemory.Select(t => $"- User asked: {t.Question} | Assistant answered: {t.Answer}"));

        var prompt = $"""

        Recent Conversation Memory:
        { memoryText }

        Answer the user's policy question using the available policy search tool.

        Rules:
        - Always search the policy documents before answering.
        - Only answer from retrieved policy evidence.
        - If no relevant evidence is found, say exactly: "I do not have grounded evidence for this."
        - Include citations from the retrieved evidence.
        - Be concise.

        User question:
        {request.Question}
        """;

        var result = await agent.RunAsync(
            prompt,
            cancellationToken: cancellationToken);

        var answer = result.ToString();

        var isGrounded = !answer.Contains(
            "I do not have grounded evidence for this.",
            StringComparison.OrdinalIgnoreCase);

        memory.AddConversation(request.Question, answer);

        return new ChatResponse(
            answer,
            ExtractCitations(answer),
            isGrounded);
    }

    private static IReadOnlyList<string> ExtractCitations(string answer)
    {
        return System.Text.RegularExpressions.Regex
            .Matches(answer, @"[A-Z]{2,}-?\d{0,4}\s§\d+(?:\.\d+)*")
            .Select(match => match.Value)
            .Distinct()
            .ToList();
    }
}