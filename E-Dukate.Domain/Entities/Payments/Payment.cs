using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.Payments;

public class Payment : Entity
{
    // Relaciones
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid SpecialistId { get; set; }
    public Specialist? Specialist { get; set; }

    // Información de pago
    public decimal SessionCost { get; set; } // Costo por sesión (65 bs)
    public int SessionCount { get; set; } // Número de sesiones
    public decimal TotalAmount { get; set; } // Total a pagar (SessionCost * SessionCount)
    public decimal AmountPaid { get; set; } // Monto pagado hasta ahora
    public decimal PendingAmount { get; set; } // Saldo pendiente (TotalAmount - AmountPaid)
    public decimal SpecialistAmount { get; set; } // 50% para especialista
    public decimal InstitutionAmount { get; set; } // 50% para institución

    // Fechas de pago
    public DateTime? FirstPaymentDate { get; set; } // Fecha del primer pago
    public DateTime? LastPaymentDate { get; set; } // Fecha del último pago

    // Estado y tipo de pago
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}