using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

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
                var admin = await _administratorRepository.GetAll().FirstOrDefaultAsync(a => a.Id == userId);
                if (admin != null)
                    fullName = admin != null ? $"{admin.Names} {admin.LastNamePaternal}" : fullName;
                break;
            case "specialist":
                var specialist = await _specialistRepository.GetAll().FirstOrDefaultAsync(s => s.Id == userId);
                if (specialist != null)
                    fullName = specialist != null ? $"{specialist.Names} {specialist.LastNamePaternal}" : fullName;
                break;
        }
        return fullName;
    }
}
