using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using HandballBackend.Models;

namespace HandballBackend;

public class HandballContext : DbContext {
    public DbSet<EloChange> EloChanges { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameEvent> GameEvents { get; set; }
    public DbSet<Official> Officials { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<PlayerGameStats> PlayerGameStats { get; set; }
    public DbSet<QuoteOfTheDay> QuotesOfTheDay { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Tournament> Tournaments { get; set; }
    public DbSet<TournamentOfficial> TournamentOfficials { get; set; }
    public DbSet<TournamentTeam> TournamentTeams { get; set; }

    public const string DbPath = "./resources/database.db";

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}