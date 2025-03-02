using Azure.Core;
using HandballBackend;
using HandballBackend.Database;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
var db = new HandballContext();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/quote", () => {
        var quotes = db.QuotesOfTheDay.ToArray();
        var index = DateTime.Today.DayOfYear % quotes.Length;
        return quotes[index].ToSendableData();
    })
    .WithName("Get QOTD")
    .WithOpenApi();

app.MapGet("/teams", () => {
    var teams = db.Teams.Where(a => a.Id != 1)
        .Include(v => v.Captain)
        .Include(v => v.NonCaptain)
        .Include(v => v.Substitute)
        .Select(t => t.ToSendableData())
        .ToArray();
    return teams;
}).WithName("Teams").WithOpenApi();

app.MapGet("/api/games/", (int? id) => {
    if (id == null) {
        return Results.BadRequest("The 'id' query parameter is required.");
    }

    var game = db.Games.Where(v => v.GameNumber == id)
        .Take(1)
        .IncludeRelevant()
        .FirstOrDefault();
    if (game is null) {
        return Results.NotFound("Game not found.");
    }

    return Results.Ok(game.ToSendableData());
}).WithName("Game").WithOpenApi();

EvilTests.EvilTest(500);

app.Run();