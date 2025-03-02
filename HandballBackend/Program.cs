using HandballBackend;
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
        return quotes[index];
    })
    .WithName("Get QOTD")
    .WithOpenApi();

app.MapGet("/teams", () => {
    var teams = db.Teams.Include(d => d.Captain).ToArray();
    return teams;
}).WithName("Teams").WithOpenApi();

app.MapGet("/game", () => {
    var game = db.Games.OrderBy(v => v.GameNumber).Include(g => g.Events).Last();
    return game.ToSendableData();
}).WithName("Game").WithOpenApi();

app.Run();