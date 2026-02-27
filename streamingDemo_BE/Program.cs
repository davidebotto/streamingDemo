// Program.cs
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);


// 1) REGISTRA i servizi CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("SseCors", policy =>
    {
        policy
            .AllowAnyOrigin()   // In produzione, restringi (es. .WithOrigins("https://tuodominio.com"))
            .AllowAnyHeader()
            .AllowAnyMethod();
        // Nota: per SSE non servono credenziali; se usi .AllowCredentials(), NON puoi usare AllowAnyOrigin
    });
});

var app = builder.Build();

// CORS se necessario (se il sito host e l'API hanno domini diversi)
app.UseCors("SseCors");

app.MapGet("/api/chat/sse", async (HttpContext ctx, string prompt, CancellationToken ct) =>
{
    ctx.Response.Headers.Append("Content-Type", "text/event-stream");
    ctx.Response.Headers.Append("Cache-Control", "no-cache");
    ctx.Response.Headers.Append("Connection", "keep-alive");


    async Task WriteEventAsync(string type, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        await ctx.Response.WriteAsync($"event: {type}\n", ct);
        await ctx.Response.WriteAsync($"data: {json}\n\n", ct);
        await ctx.Response.Body.FlushAsync(ct);
    }


    try
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "answer.json");
        var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath, ct);
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        // Stream answer_delta token per token
        if (root.TryGetProperty("answer", out var answerEl))
        {
            var answer = answerEl.GetString() ?? string.Empty;
            // Simula lo streaming dividendo per parole
            var tokens = answer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                await WriteEventAsync("answer_delta", new { content = token + " " });
                await Task.Delay(50, ct); // simula latenza LLM
            }
        }

        // relevant_sources
        if (root.TryGetProperty("relevantSources", out var sourcesEl))
        {
            var sources = sourcesEl.EnumerateArray()
                .Select(s => s.GetString())
                .ToArray();
            await WriteEventAsync("relevantSources", new { items = sources });
        }

        // tips
        if (root.TryGetProperty("tips", out var tipsEl))
        {
            var title = tipsEl.GetProperty("title").GetString();
            var tokens = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                await WriteEventAsync("tips_delta", new { content = token + " " });
                await Task.Delay(50, ct); // simula latenza LLM
            }

            var items = tipsEl.GetProperty("items")
                .EnumerateArray()
                .Select(i => i.GetString())
                .ToArray();
            await WriteEventAsync("tips", new { items });
        }

        // opzionale: evento di chiusura
        await WriteEventAsync("done", new { ok = true });
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
        await WriteEventAsync("error", new { code = "SERVER_ERROR", message = ex.Message });
    }

    ////// opzionale: disabilita buffering intermedio
    
});

app.Run();

static async IAsyncEnumerable<string> GenerateAsync(string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
{
    var tokens = new[] { "Certo, ", "posso ", "aiutarti ", "con ", "il ", "tuo ", "prompt: ", prompt, " ✅" };
    foreach (var t in tokens)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(250, ct);
        yield return t;
    }
}