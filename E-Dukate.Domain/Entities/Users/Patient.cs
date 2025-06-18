using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Entities.MedicalHistories;
using E_Dukate.Domain.Entities.Payments;

namespace E_Dukate.Domain.Entities.Users;

public class Patient : User
{
    public Guid MedicalHistoryId { get; set; }
    public MedicalHistory? MedicalHistory { get; set; }
    public List<Appointment>? Appointments { get; set; } = new List<Appointment>();
    public List<Payment>? Payments { get; set; } = new List<Payment>();
}