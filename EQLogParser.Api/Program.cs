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

    statusStore.Set(status);
    await hubContext.Clients.All.SendAsync("statusUpdated", status);

    return Results.Accepted();
});

app.MapHub<StatusHub>("/hubs/status");

app.Run();
