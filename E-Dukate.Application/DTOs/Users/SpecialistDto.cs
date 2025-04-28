using E_Dukate.Application.DTOs.Schedules;

namespace E_Dukate.Application.DTOs.Users;

public class SpecialistDto : BaseUserDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string TypeOfSpecialty { get; set; }
    public required int YearsOfExperience { get; set; }
    public required string SpecialistCode { get; set; }
    public List<ScheduleDto> Schedules { get; set; } = new List<ScheduleDto>();
}