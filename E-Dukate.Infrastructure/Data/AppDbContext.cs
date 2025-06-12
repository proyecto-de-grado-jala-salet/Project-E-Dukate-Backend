using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Entities.Schedules;
using E_Dukate.Domain.Entities.Auth;
using E_Dukate.Domain.Entities.MedicalHistories;
using E_Dukate.Domain.Entities.FAQ;

namespace E_Dukate.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Administrator> Administrators { get; set; }
    public DbSet<Specialist> Specialists { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Specialty> Specialties { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<LoginLog> LoginLogs { get; set; }
    public DbSet<UserAuth> UserAuths { get; set; }
    public DbSet<MedicalHistory> MedicalHistories { get; set; }
    public DbSet<MedicalHistoryPermission> MedicalHistoryPermissions { get; set; }
    public DbSet<MedicalConsultation> MedicalConsultations { get; set; }
    public DbSet<Faq> Faqs { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrator>().ToTable("Administrators").HasKey(a => a.Id);
        modelBuilder.Entity<Specialist>().ToTable("Specialists").HasKey(s => s.Id);
        modelBuilder.Entity<Patient>().ToTable("Patients").HasKey(p => p.Id);
        modelBuilder.Entity<Specialty>().ToTable("Specialties").HasKey(s => s.Id);
        modelBuilder.Entity<Schedule>().ToTable("Schedules").HasKey(s => s.Id);
        modelBuilder.Entity<TimeSlot>().ToTable("TimeSlots").HasKey(ts => ts.Id);
        modelBuilder.Entity<LoginLog>().ToTable("LoginLogs").HasKey(l => l.Id);
        modelBuilder.Entity<UserAuth>().ToTable("UserAuths").HasKey(u => u.Id);
        modelBuilder.Entity<MedicalHistory>().ToTable("MedicalHistories").HasKey(mh => mh.Id);
        modelBuilder.Entity<MedicalHistoryPermission>().ToTable("MedicalHistoryPermissions").HasKey(p => p.Id);
        modelBuilder.Entity<MedicalConsultation>().ToTable("MedicalConsultations").HasKey(c => c.Id);
        modelBuilder.Entity<Faq>().ToTable("Faqs").HasKey(f => f.Id);
        
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.MedicalHistory)
            .WithOne(mh => mh.Patient)
            .HasForeignKey<MedicalHistory>(mh => mh.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalHistory>()
            .HasMany(mh => mh.Permissions)
            .WithOne(p => p.MedicalHistory)
            .HasForeignKey(p => p.MedicalHistoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalHistoryPermission>()
            .HasMany(p => p.Consultations)
            .WithOne(c => c.Permission)
            .HasForeignKey(c => c.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalHistoryPermission>()
            .HasOne(p => p.Specialist)
            .WithMany()
            .HasForeignKey(p => p.SpecialistId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicalConsultation>()
            .HasOne(c => c.Specialist)
            .WithMany()
            .HasForeignKey(c => c.SpecialistId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Specialist>()
            .HasOne(s => s.Specialty)
            .WithMany()
            .HasForeignKey("SpecialtyId");

        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Specialist)
            .WithMany(s => s.Schedules)
            .HasForeignKey(s => s.SpecialistId);

        modelBuilder.Entity<TimeSlot>()
            .HasOne(ts => ts.Schedule)
            .WithMany(s => s.TimeSlots)
            .HasForeignKey(ts => ts.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserAuth>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}