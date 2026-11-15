using System.ComponentModel.DataAnnotations;
using Dwellio.Core.Geo;
using Dwellio.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Dwellio.Web.Data;

public class ComplexEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Description { get; set; } = "";
    public Complex ToModel() => new(Id, Name, Address, new GeoPoint(Latitude, Longitude), Description);
}

/// <summary>Where a listing comes from. Shown on every card so real and illustrative prices are never confused.</summary>
public enum ListingKind { Sourced, Owner, Sample }

public class ListingEntity
{
    public int Id { get; set; }
    public int ComplexId { get; set; }
    public ListingKind Kind { get; set; } = ListingKind.Owner;
    [Required, StringLength(100, MinimumLength = 5)] public string Title { get; set; } = "";
    [Required, StringLength(2000, MinimumLength = 20)] public string Description { get; set; } = "";
    [Range(10000, 5000000)] public decimal Rent { get; set; } = 180000;
    [Range(0, 1000000)] public decimal? Utilities { get; set; }
    [Range(0, 10000000)] public decimal? Deposit { get; set; }
    [Range(1, 6)] public int Rooms { get; set; } = 1;
    [Range(10, 500)] public int Area { get; set; } = 40;
    [Range(1, 80)] public int Floor { get; set; } = 4;
    public bool Furnished { get; set; } = true;
    public bool Available { get; set; } = true;
    public bool Hidden { get; set; }
    public string OwnerId { get; set; } = "";
    [StringLength(150)] public string Contact { get; set; } = "";
    /// <summary>Local upload, bundled illustration or (for sourced listings) the provider's photo URL.</summary>
    public string Photo { get; set; } = "/images/home-1.svg";
    [StringLength(400)] public string? SourceUrl { get; set; }
    [StringLength(400)] public string? Evidence { get; set; }
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public Apartment ToModel() => new(Id, ComplexId, Title, Rent, Utilities, Deposit, Rooms, Area, Furnished, Available);
}

public class FavoriteEntity { public string UserId { get; set; } = ""; public int ListingId { get; set; } }
public class ProfileEntity { public string UserId { get; set; } = ""; public string PreferencesJson { get; set; } = "{}"; }

public class ReviewEntity
{
    public int Id { get; set; }
    public int ComplexId { get; set; }
    public string UserId { get; set; } = "";
    public string Author { get; set; } = "";
    [Required, StringLength(1000, MinimumLength = 10)] public string Text { get; set; } = "";
    [Range(1, 5)] public int Cleanliness { get; set; } = 4;
    [Range(1, 5)] public int Quietness { get; set; } = 4;
    [Range(1, 5)] public int Facilities { get; set; } = 4;
    public bool Hidden { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Last successful live OpenStreetMap answer for an area, so restarts and outages keep fresh data.</summary>
public class PlaceCacheEntity
{
    public string Key { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }
    public DateTime FetchedUtc { get; set; }
    public string PlacesJson { get; set; } = "";
}

public class HousingDbContext(DbContextOptions<HousingDbContext> options) : DbContext(options)
{
    /// <summary>Bump when entities change. The local database is rebuilt when its version differs.</summary>
    public const int SchemaVersion = 2;

    public DbSet<ComplexEntity> Complexes => Set<ComplexEntity>();
    public DbSet<ListingEntity> Listings => Set<ListingEntity>();
    public DbSet<FavoriteEntity> Favorites => Set<FavoriteEntity>();
    public DbSet<ProfileEntity> Profiles => Set<ProfileEntity>();
    public DbSet<ReviewEntity> Reviews => Set<ReviewEntity>();
    public DbSet<PlaceCacheEntity> PlaceCache => Set<PlaceCacheEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<FavoriteEntity>().HasKey(x => new { x.UserId, x.ListingId });
        b.Entity<ProfileEntity>().HasKey(x => x.UserId);
        b.Entity<PlaceCacheEntity>().HasKey(x => x.Key);
        b.Entity<ListingEntity>().Property(x => x.Kind).HasConversion<string>();
        b.Entity<ListingEntity>().HasIndex(x => new { x.ComplexId, x.Available });
        b.Entity<ListingEntity>().HasOne<ComplexEntity>().WithMany().HasForeignKey(x => x.ComplexId);
        b.Entity<FavoriteEntity>().HasOne<ListingEntity>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ReviewEntity>().HasOne<ComplexEntity>().WithMany().HasForeignKey(x => x.ComplexId);
        b.Entity<ReviewEntity>().HasIndex(x => new { x.UserId, x.ComplexId }).IsUnique();
    }

    /// <summary>Creates the database, rebuilding it if it was made by an older version of Dwellio.</summary>
    public async Task EnsureCurrentSchemaAsync(CancellationToken ct = default)
    {
        var connection = Database.GetDbConnection();
        await Database.OpenConnectionAsync(ct);
        try
        {
            await using var read = connection.CreateCommand();
            read.CommandText = "PRAGMA user_version;";
            var version = Convert.ToInt32(await read.ExecuteScalarAsync(ct));
            if (version != SchemaVersion) await Database.EnsureDeletedAsync(ct);
        }
        finally { await Database.CloseConnectionAsync(); }

        if (await Database.EnsureCreatedAsync(ct))
            await Database.ExecuteSqlRawAsync($"PRAGMA user_version = {SchemaVersion};", ct);
    }
}
