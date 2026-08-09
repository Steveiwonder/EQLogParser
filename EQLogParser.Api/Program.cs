using EQLogParser.Api;
using EQLogParser.Contracts;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<StatusStore>();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", (StatusStore statusStore) =>
{
    return statusStore.Current == null
        ? Results.NoContent()
        : Results.Ok(statusStore.Current);
});

app.MapPost("/api/status", async (
    ParserStatusUpdate status,
    StatusStore statusStore,
    IHubContext<StatusHub> hubContext) =>
{
    if (status.UpdatedAt == default)
    {
        status.UpdatedAt = DateTimeOffset.Now;
    }

    ParserStatusUpdate filteredStatus = statusStore.Set(status);
    await hubContext.Clients.All.SendAsync("statusUpdated", filteredStatus);

    return Results.Accepted();
});

app.MapPost("/api/status/dismiss-buff", async (
    DismissBuffRequest request,
    StatusStore statusStore,
    IHubContext<StatusHub> hubContext) =>
{
    ParserStatusUpdate? status = statusStore.Dismiss(request);
    if (status != null)
    {
        await hubContext.Clients.All.SendAsync("statusUpdated", status);
    }

    return Results.NoContent();
});

app.MapHub<StatusHub>("/hubs/status");

app.Run();
