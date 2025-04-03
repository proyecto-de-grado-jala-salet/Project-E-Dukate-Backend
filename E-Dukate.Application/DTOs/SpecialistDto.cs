namespace E_Dukate.Application.DTOs;

public class SpecialistDto
{
    public required string Names { get; set; }
    public required string LastNamePaternal { get; set; }
    public required string LastNameMaternal { get; set; }
    public required string MobileNumber { get; set; }
    public required int IdentityCard { get; set; }
    public string? PhoneNumber { get; set; }
    public required int Age { get; set; }
    public required string Gender { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string Address { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Specialty { get; set; }
    public required int YearsOfExperience { get; set; }
    public required string SpecialistCode { get; set; }
    public required string AccessCode { get; set; }
}