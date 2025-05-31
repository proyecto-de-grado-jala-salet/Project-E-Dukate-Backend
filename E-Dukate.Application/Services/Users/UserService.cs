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
            LastNameMaternal = a.LastNameMaternal ?? string.Empty,
            MobileNumber = a.MobileNumber,
            Role = "Administrator"
        });

        var specialists = _specialistRepository.GetAll().Select(s => new UserDto
        {
            Id = s.Id,
            Names = s.Names,
            LastNamePaternal = s.LastNamePaternal,
            LastNameMaternal = s.LastNameMaternal ?? string.Empty,
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
    
    public async Task<(IEnumerable<UserDto> Items, int TotalCount)> SearchUsersAsync(string searchTerm, PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllUsersAsync(pagination);
        }
        searchTerm = searchTerm.ToLower();

        var admins = _adminRepository.GetAll()
            .Where(a =>
                a.Names.ToLower().Contains(searchTerm) ||
                a.LastNamePaternal.ToLower().Contains(searchTerm) ||
                (a.LastNameMaternal != null && a.LastNameMaternal.ToLower().Contains(searchTerm)) ||
                a.MobileNumber.Contains(searchTerm) ||
                a.IdentityCard.ToString().Contains(searchTerm) ||
                a.Age.ToString().Contains(searchTerm) ||
                a.Gender.ToLower().Contains(searchTerm)
            )
            .Select(a => new UserDto
            {
                Id = a.Id,
                Names = a.Names,
                LastNamePaternal = a.LastNamePaternal,
                LastNameMaternal = a.LastNameMaternal ?? string.Empty,
                MobileNumber = a.MobileNumber,
                Role = "Administrator"
            });
        
        var specialists = _specialistRepository.GetAll()
            .Where(s =>
                s.Names.ToLower().Contains(searchTerm) ||
                s.LastNamePaternal.ToLower().Contains(searchTerm) ||
                (s.LastNameMaternal != null && s.LastNameMaternal.ToLower().Contains(searchTerm)) ||
                s.MobileNumber.Contains(searchTerm) ||
                s.IdentityCard.ToString().Contains(searchTerm) ||
                s.Age.ToString().Contains(searchTerm) ||
                s.Gender.ToLower().Contains(searchTerm) ||
                s.Specialty.TypeOfSpecialty.ToLower().Contains(searchTerm) ||
                s.SpecialistCode.ToLower().Contains(searchTerm)
            )
            .Select(s => new UserDto
            {
                Id = s.Id,
                Names = s.Names,
                LastNamePaternal = s.LastNamePaternal,
                LastNameMaternal = s.LastNameMaternal ?? string.Empty,
                MobileNumber = s.MobileNumber,
                Role = "Specialist"
            });
        
        var allUsersQuery = admins.Concat(specialists);
        
        var totalCount = await allUsersQuery.CountAsync();
        var items = await allUsersQuery
            .OrderBy(u => u.Names)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}