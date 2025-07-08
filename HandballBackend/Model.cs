using System;
using System.Collections.Generic;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;
using HandballBackend.FixtureGenerator;
using HandballBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend;

public class HandballContext : DbContext {
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

    public readonly string ConnectionString = File.ReadAllText(
        Config.SECRETS_FOLDER + "/DatabaseConnection.txt"
    );

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<PlayerGameStats>()
            .HasOne(pgs => pgs.Team)
            .WithMany(t => t.PlayerGameStats)
            .HasForeignKey(pgs => pgs.TeamId);

        modelBuilder
            .Entity<Game>()
            .HasOne(g => g.Official)
            .WithMany(o => o.Games)
            .HasForeignKey(pgs => pgs.OfficialId);

        modelBuilder
            .Entity<PlayerGameStats>()
            .HasOne(pgs => pgs.Opponent)
            .WithMany()
            .HasForeignKey(pgs => pgs.OpponentId);
        modelBuilder
            .Entity<GameEvent>()
            .Property(e => e.EventType)
            .HasConversion(
                v => Utilities.SplitCamelCase(v.ToString()),
                v => (GameEventType) Enum.Parse(typeof(GameEventType), v.Replace(" ", ""))
            );
        modelBuilder
            .Entity<Person>()
            .Property(e => e.PhoneNumber)
            .HasConversion(
                v => v == null ? null : EncryptionHelper.Encrypt(v),
                v => v == null ? null : EncryptionHelper.Decrypt(v)
            );
        modelBuilder.Entity<GameEvent>().HasOne(gE => gE.Player).WithMany(p => p.Events);
        modelBuilder.Entity<GameEvent>().HasOne(gE => gE.Game).WithMany(g => g.Events);
        modelBuilder.Entity<GameEvent>().HasOne(gE => gE.TeamOneLeft);

        modelBuilder
            .Entity<TournamentTeam>()
            .HasOne(gE => gE.Team)
            .WithMany(g => g.TournamentTeams)
            .HasForeignKey(g => g.TeamId);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options) {
        if (Config.USING_POSTGRES) {
            options.UseNpgsql(ConnectionString);
        } else {
            options.UseSqlite(ConnectionString);
        }
    }
}