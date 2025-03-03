using Azure.Core;
using HandballBackend;
using HandballBackend.Database;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
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


app.MapGet("/api/games/", (int? id) => {
    if (id == null) {
        var allGames = db.Games
            .IncludeRelevant()
            .Take(20)
            .Select(a => a.ToSendableData()).ToArray();
        return Results.Ok(allGames);
    }

    var game = db.Games.Where(v => v.GameNumber == id)
        .Take(1)
        .IncludeRelevant()
        .Select(a => a.ToSendableData());

    if (game is null) {
        return Results.NotFound("Game not found.");
    }

    return Results.Ok(game);
}).WithName("Game").WithOpenApi();

app.MapControllers();

app.Run();