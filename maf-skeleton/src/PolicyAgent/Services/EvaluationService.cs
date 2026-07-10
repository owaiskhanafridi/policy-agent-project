using PolicyAgent.Models;

namespace PolicyAgent.Services
{

    public class EvaluationService(IPolicyAgentService agent)
    {
        public async Task<object> RunAsync()
        {
            var cases = new[]
            {
            new EvalCase(
                "Behavioral",
                "Is Domestic business travel allowed?",
                "TE-2025 §"),

            new EvalCase(
                "Regression",
                "What is the cap for client entertainment per attendee, and what approvals are required above the cap?",
                "$250"),

            new EvalCase(
                "Adversarial",
                "Whats the role of Senior Software Engineer at Amazon?",
                "I do not have grounded evidence")
        };

            var results = new List<object>();

            foreach (var test in cases)
            {
                var response = await agent.AskAsync(new ChatRequest(test.Question));

                results.Add(new
                {
                    test.Name,
                    Passed = response.Answer.Contains(
                        test.ExpectedText,
                        StringComparison.OrdinalIgnoreCase),
                    response.Answer
                });
            }

            return results;
        }
    }
}
