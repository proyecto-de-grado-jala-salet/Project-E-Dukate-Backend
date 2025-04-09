using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;

namespace E_Dukate.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Administrator> Administrators { get; set; }
    public DbSet<Specialist> Specialists { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Specialty> Specialties { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrator>().ToTable("Administrators").HasKey(a => a.Id);
        modelBuilder.Entity<Specialist>().ToTable("Specialists").HasKey(s => s.Id);
        modelBuilder.Entity<Patient>().ToTable("Patients").HasKey(p => p.Id);
        modelBuilder.Entity<Specialty>().ToTable("Specialties").HasKey(s => s.Id);
        
        modelBuilder.Entity<Specialist>()
            .HasOne(s => s.Specialty)
            .WithMany()
            .HasForeignKey("SpecialtyId");
    }
}