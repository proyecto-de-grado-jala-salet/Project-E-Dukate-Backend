namespace E_Dukate.Domain.Entities;

public class Specialist : User
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required Specialty Specialty { get; set; }
    public required int YearsOfExperience { get; set; }
    public required string SpecialistCode { get; set; }
}