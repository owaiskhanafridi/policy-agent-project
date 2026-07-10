namespace PolicyAgent.Models
{
    public record ConversationHistory(
    string Question,
    string Answer,
    DateTimeOffset Timestamp);
}
