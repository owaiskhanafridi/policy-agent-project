// PolicyAgent entry point
//
// Skeleton. Your agent registration goes here. The csproj references MAF +
// Microsoft.Extensions.AI.Evaluation; add your model client (any
// Microsoft.Extensions.AI `IChatClient`) and its packages. We are
// provider-agnostic. See "Model access" in the skeleton README for free,
// zero-setup options. Add what you need; remove what you do not.

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using PolicyAgent.Repositories;
using PolicyAgent.Services;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Configuration
// ============================================================================
// Wire any Microsoft.Extensions.AI `IChatClient`. Bring whatever provider you
// already use; if you want a free, zero-setup option, GitHub Models works with
// the GitHub account you'll submit from (OpenAI-compatible endpoint + a PAT).
// Note that its free tier is daily-rate-limited (see the README), so a local
// model or a smaller catalog model is easier for heavy iteration. Any capable
// model is fine. Configure endpoint / key / model however you like, e.g.:
//   Model:Endpoint   (OpenAI-compatible base URL)
//   Model:ApiKey     (from user-secrets / env, do not commit)
//   Model:Name       (e.g., gpt-4o-mini)

// ============================================================================
// Services

builder.Services.AddSingleton<IPolicyRepository, PolicyRepository>();
builder.Services.AddSingleton<IPolicySearchService, EmbeddingPolicySearchService>();
builder.Services.AddSingleton<PolicySearchTool>();
builder.Services.AddSingleton<ConversationMemoryService>();
builder.Services.AddScoped<IPolicyAgentService, PolicyAgentService>();
builder.Services.AddScoped<EvaluationService>();

// ============================================================================
// Register your MAF agent, tools, memory provider, eval runner here.


builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ============================================================================
// HTTP surface
// ============================================================================


var ollamaUri = new Uri(
    builder.Configuration["Ollama:Endpoint"]
    ?? "http://localhost:11434");


// Chat model for the MAF agent / answer generation
//builder.Services.AddSingleton<IChatClient>(_ =>
//{
//    var client = new OllamaApiClient(ollamaUri, "qwen3");
//    return client;
//});


//Fix for missing MessageId issue
builder.Services.AddSingleton<IChatClient>(_ =>
{
    IChatClient ollamaClient =
        new OllamaApiClient(ollamaUri, "qwen3");

    return ollamaClient
        .AsBuilder()
        .Use(
            getResponseFunc: null,
            getStreamingResponseFunc: AddMessageIdsAsync)
        .Build();
});


// Embedding model for policy search / RAG
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ =>
    new OllamaApiClient(ollamaUri, "nomic-embed-text"));

builder.Services.AddSingleton<AIAgent>(sp =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var policySearchTool = sp.GetRequiredService<PolicySearchTool>();

    var searchPolicyTool = AIFunctionFactory.Create(
    policySearchTool.SearchAsync,
    name: "search_policy",
    description: "Searches company policy documents and returns relevant grounded evidence with citations.");

    return new ChatClientAgent(
        chatClient,
        instructions: """
        You are PolicyAgent, a policy assistant.

        Use the policy search tool before answering policy questions.
        If the tool does not return relevant evidence, say:
        "I do not have grounded evidence for this."

        Always base answers on retrieved policy evidence.
        Include citations when available.
        """,
        tools: [searchPolicyTool]);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var agent = app.Services.GetRequiredService<AIAgent>();

app.MapAGUI("/ag-ui", agent);

app.UseCors("Frontend");

// app.MapPost("/chat", async (HttpContext ctx, ...) => { ... });

app.MapControllers();

//Fix for missing MessageId issue
static async IAsyncEnumerable<ChatResponseUpdate> AddMessageIdsAsync(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options,
    IChatClient innerChatClient,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var messageId = $"msg_{Guid.NewGuid():N}";

    await foreach (var update in innerChatClient.GetStreamingResponseAsync(
        messages,
        options,
        cancellationToken))
    {
        if (string.IsNullOrWhiteSpace(update.MessageId))
        {
            update.MessageId = messageId;
        }

        yield return update;
    }
}

app.Run();
