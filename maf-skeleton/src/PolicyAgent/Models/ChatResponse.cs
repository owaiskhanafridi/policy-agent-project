namespace PolicyAgent.Models
{
    public record ChatResponse(string Answer, IReadOnlyList<string> Citations, bool IsGrounded);
}
