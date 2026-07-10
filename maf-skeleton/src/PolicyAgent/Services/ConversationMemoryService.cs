using global::PolicyAgent.Models;

namespace PolicyAgent.Services
{
    public class ConversationMemoryService
    {
        private readonly List<ConversationHistory> _conversations = [];

        public void AddConversation(string question, string answer)
        {
            _conversations.Add(new ConversationHistory(question, answer, DateTimeOffset.UtcNow));

            if (_conversations.Count > 20)
            {
                _conversations.RemoveAt(0);
            }
        }

        public IReadOnlyList<ConversationHistory> GetRecentConversations(int count = 5)
        {
            return _conversations
                .TakeLast(count)
                .ToList();
        }
    }
}
