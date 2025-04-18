using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Application.DTOs.Common;

namespace E_Dukate.Application.Services.Users;

public class UserService
{
    private readonly IGenericRepository<Administrator> _adminRepository;
    private readonly IGenericRepository<Specialist> _specialistRepository;

    public UserService(
        IGenericRepository<Administrator> adminRepository,
        IGenericRepository<Specialist> specialistRepository)
    {
        _adminRepository = adminRepository;
        _specialistRepository = specialistRepository;
    }

    public async Task<(IEnumerable<UserDto> Items, int TotalCount)> GetAllUsersAsync(PaginationParams pagination)
    {
        var admins = _adminRepository.GetAll().Select(a => new UserDto
        {
            Id = a.Id,
            Names = a.Names,
            LastNamePaternal = a.LastNamePaternal,
            LastNameMaternal = a.LastNameMaternal,
            MobileNumber = a.MobileNumber,
            Role = "Administrator"
        });

        var specialists = _specialistRepository.GetAll().Select(s => new UserDto
        {
            Id = s.Id,
            Names = s.Names,
            LastNamePaternal = s.LastNamePaternal,
            LastNameMaternal = s.LastNameMaternal,
            MobileNumber = s.MobileNumber,
            Role = "Specialist"
        });

        var allUsersQuery = admins.Concat(specialists);
        
        var totalCount = await allUsersQuery.CountAsync();
        var items = await allUsersQuery
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public void DeleteUser(Guid id, string role)
    {
        switch (role)
        {
            case "Administrator":
                _adminRepository.Delete(id);
                break;
            case "Specialist":
                _specialistRepository.Delete(id);
                break;
            default:
                throw new Exception("Invalid role specified.");
        }
    }
}