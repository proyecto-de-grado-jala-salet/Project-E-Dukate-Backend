using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.Payments;

public class Payment : Entity
{
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid SpecialistId { get; set; }
    public Specialist? Specialist { get; set; }
    public decimal SessionCost { get; set; }
    public int SessionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal SpecialistAmount { get; set; }
    public decimal InstitutionAmount { get; set; }
    public DateTime? FirstPaymentDate { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}