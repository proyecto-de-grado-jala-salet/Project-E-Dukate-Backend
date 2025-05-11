using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.Interfaces.Auth;

namespace E_Dukate.Application.Services.Auth;
public class UserProfileService : IUserProfileService
{
    private readonly IGenericRepository<Administrator> _administratorRepository;
    private readonly IGenericRepository<Specialist> _specialistRepository;

    public UserProfileService(
        IGenericRepository<Administrator> administratorRepository,
        IGenericRepository<Specialist> specialistRepository)
    {
        _administratorRepository = administratorRepository;
        _specialistRepository = specialistRepository;
    }

    public async Task<string> GetFullNameAsync(Guid userId, string role)
    {
        string fullName = "Usuario Desconocido";
        switch (role.ToLower())
        {
            case "administrator":
                var admin = _administratorRepository.GetAll().FirstOrDefault(a => a.Id == userId);
                if (admin != null)
                    fullName = $"{admin.Names} {admin.LastNamePaternal}";
                break;
            case "specialist":
                var specialist = _specialistRepository.GetAll().FirstOrDefault(s => s.Id == userId);
                if (specialist != null)
                    fullName = $"{specialist.Names} {specialist.LastNamePaternal}";
                break;
        }
        return fullName;
    }
}
