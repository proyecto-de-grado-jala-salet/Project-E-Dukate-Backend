using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Application.Services.WhatsApp;

public class ConversationState
{
    public ConversationStep Step { get; set; }
    public Patient? PatientData { get; set; }
    public Guid SelectedSpecialtyId { get; set; }
    public Guid SelectedSpecialistId { get; set; }
    public int SchedulePageIndex { get; set; }
    public List<(string SlotId, string Title, string Description)> AvailableSchedules { get; set; } = new();
}