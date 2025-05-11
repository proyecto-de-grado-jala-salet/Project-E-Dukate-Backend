using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Entities.Schedules;
using E_Dukate.Domain.Entities.Auth;

namespace E_Dukate.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Administrator> Administrators { get; set; }
    public DbSet<Specialist> Specialists { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Specialty> Specialties { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<LoginLog> LoginLogs { get; set; }
    public DbSet<UserAuth> UserAuths { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrator>().ToTable("Administrators").HasKey(a => a.Id);
        modelBuilder.Entity<Specialist>().ToTable("Specialists").HasKey(s => s.Id);
        modelBuilder.Entity<Patient>().ToTable("Patients").HasKey(p => p.Id);
        modelBuilder.Entity<Specialty>().ToTable("Specialties").HasKey(s => s.Id);
        modelBuilder.Entity<Schedule>().ToTable("Schedules").HasKey(s => s.Id);
        modelBuilder.Entity<LoginLog>().ToTable("LoginLogs").HasKey(l => l.Id);
        modelBuilder.Entity<UserAuth>().ToTable("UserAuths").HasKey(u => u.Id);

        modelBuilder.Entity<Specialist>()
            .HasOne(s => s.Specialty)
            .WithMany()
            .HasForeignKey("SpecialtyId");

        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Specialist)
            .WithMany(s => s.Schedules)
            .HasForeignKey(s => s.SpecialistId);

        modelBuilder.Entity<Schedule>()
            .Property(s => s.TimeSlots)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<TimeSlot>>(v, (JsonSerializerOptions)null),
                new ValueComparer<List<TimeSlot>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.StartTime.GetHashCode(), v.EndTime.GetHashCode())),
                    c => c.ToList()
                ));
                
        modelBuilder.Entity<UserAuth>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}