using System.ComponentModel.DataAnnotations;
using System.Collections.Immutable;
using System.Text.Json;
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
    public string AmenitiesJson { get; set; } = "[]";
    public string Photo { get; set; } = "/images/home-1.svg";
    public Complex ToModel() => new(Id, Name, Address, Latitude, Longitude,
        (JsonSerializer.Deserialize<Amenity[]>(AmenitiesJson) ?? []).ToImmutableArray(), Description);
}
public class ListingEntity
{
    public int Id { get; set; }
    public int ComplexId { get; set; }
    [Required, StringLength(100, MinimumLength = 5)] public string Title { get; set; } = "";
    [Required, StringLength(2000, MinimumLength = 20)] public string Description { get; set; } = "";
    [Range(10000, 5000000)] public decimal Rent { get; set; } = 180000;
    [Range(0, 1000000)] public decimal? Utilities { get; set; }
    [Range(0, 10000000)] public decimal Deposit { get; set; }
    [Range(1, 6)] public int Rooms { get; set; } = 1;
    [Range(10, 500)] public int Area { get; set; } = 40;
    [Range(1, 80)] public int Floor { get; set; } = 4;
    public bool Furnished { get; set; } = true;
    public bool Available { get; set; } = true;
    public bool Hidden { get; set; }
    public bool IsDemo { get; set; }
    public string OwnerId { get; set; } = "";
    [StringLength(150)] public string Contact { get; set; } = "";
    public string Photo { get; set; } = "/images/home-1.svg";
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
public class HousingDbContext(DbContextOptions<HousingDbContext> options) : DbContext(options)
{
    public DbSet<ComplexEntity> Complexes => Set<ComplexEntity>();
    public DbSet<ListingEntity> Listings => Set<ListingEntity>();
    public DbSet<FavoriteEntity> Favorites => Set<FavoriteEntity>();
    public DbSet<ProfileEntity> Profiles => Set<ProfileEntity>();
    public DbSet<ReviewEntity> Reviews => Set<ReviewEntity>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<FavoriteEntity>().HasKey(x => new { x.UserId, x.ListingId });
        b.Entity<ProfileEntity>().HasKey(x => x.UserId);
        b.Entity<ListingEntity>().HasIndex(x => new { x.ComplexId, x.Available });
        b.Entity<ListingEntity>().HasOne<ComplexEntity>().WithMany().HasForeignKey(x => x.ComplexId);
        b.Entity<FavoriteEntity>().HasOne<ListingEntity>().WithMany().HasForeignKey(x => x.ListingId);
        b.Entity<ReviewEntity>().HasOne<ComplexEntity>().WithMany().HasForeignKey(x => x.ComplexId);
        b.Entity<ReviewEntity>().HasIndex(x => new { x.UserId, x.ComplexId }).IsUnique();
    }
}
