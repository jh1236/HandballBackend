using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using HandballBackend.Database.Models;
using HandballBackend.Utils;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        // Configure the one-to-many relationship between Team and PlayerGameStats
        modelBuilder.Entity<PlayerGameStats>()
            .HasOne(pgs => pgs.Team)
            .WithMany(t => t.PlayerGameStats)
            .HasForeignKey(pgs => pgs.TeamId);

        // Configure other relationships if needed
        modelBuilder.Entity<PlayerGameStats>()
            .HasOne(pgs => pgs.Opponent)
            .WithMany()
            .HasForeignKey(pgs => pgs.OpponentId);
        modelBuilder
            .Entity<GameEvent>()
            .Property(e => e.EventType)
            .HasConversion(
                v => Utilities.SplitCamelCase(v.ToString()),
                v => (GameEventType) Enum.Parse(typeof(GameEventType), v.Replace(" ", "")));
        modelBuilder
            .Entity<GameEvent>()
            .HasOne(gE => gE.Player)
            .WithMany(
                p => p.Events
            );
        modelBuilder
            .Entity<GameEvent>()
            .HasOne(gE => gE.Game)
            .WithMany(
                g => g.Events
            );
        modelBuilder
            .Entity<GameEvent>()
            .HasOne(gE => gE.TeamOneLeft);
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}