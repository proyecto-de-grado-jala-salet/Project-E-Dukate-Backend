using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Entities.Schedules;
using E_Dukate.Domain.Entities.Auth;
using E_Dukate.Domain.Entities.MedicalHistories;
using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Entities.Payments;

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
    public DbSet<MedicalDocument> MedicalDocuments { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<ScheduledSession> ScheduledSessions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentQR> PaymentQRs { get; set; }

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
        modelBuilder.Entity<Appointment>().ToTable("Appointments").HasKey(a => a.Id);
        modelBuilder.Entity<ScheduledSession>().ToTable("ScheduledSessions").HasKey(ss => ss.Id);
        modelBuilder.Entity<Payment>().ToTable("Payments").HasKey(p => p.Id);
        modelBuilder.Entity<PaymentQR>().ToTable("PaymentQRs").HasKey(qr => qr.Id);

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
            .HasForeignKey(s => s.SpecialistId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TimeSlot>()
            .HasOne(ts => ts.Schedule)
            .WithMany(s => s.TimeSlots)
            .HasForeignKey(ts => ts.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserAuth>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Specialist)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.SpecialistId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Specialty)
            .WithMany()
            .HasForeignKey(a => a.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Payment)
            .WithOne(p => p.Appointment)
            .HasForeignKey<Payment>(p => p.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScheduledSession>()
            .HasOne(ss => ss.Appointment)
            .WithMany(a => a.ScheduledSessions)
            .HasForeignKey(ss => ss.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScheduledSession>()
            .HasOne(ss => ss.TimeSlot)
            .WithMany()
            .HasForeignKey(ss => ss.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Patient)
            .WithMany(p => p.Payments)
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Specialist)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SpecialistId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScheduledSession>()
            .Property(ss => ss.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Payment>()
            .Property(p => p.Status)
            .HasConversion<string>();

        modelBuilder.Entity<PaymentQR>()
            .HasIndex(qr => qr.FilePath)
            .IsUnique();
    }
}