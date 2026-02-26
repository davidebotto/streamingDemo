// Program.cs
using System.Text;
using System.Text.Json;

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

app.MapGet("/api/chat/sse", async (HttpContext ctx, string prompt) =>
{
    ctx.Response.Headers.Append("Content-Type", "text/event-stream");
    ctx.Response.Headers.Append("Cache-Control", "no-cache");
    ctx.Response.Headers.Append("Connection", "keep-alive");

    // opzionale: disabilita buffering intermedio
    ctx.Response.Headers.Append("X-Accel-Buffering", "no");

    // Writer senza chiudere lo stream della Response
    using var writer = new StreamWriter(ctx.Response.Body, Encoding.UTF8, leaveOpen: true);

    // Esempio: invio "token" in streaming
    await foreach (var token in GenerateAsync(prompt, ctx.RequestAborted))
    {
        var payload = JsonSerializer.Serialize(new { content = token });
        await writer.WriteAsync($"data: {payload}\n\n");
        await writer.FlushAsync(); // IMPORTANTE: flush ad ogni chunk
    }

    // opzionale: evento di chiusura
    await writer.WriteAsync("event: end\ndata: {}\n\n");
    await writer.FlushAsync();
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