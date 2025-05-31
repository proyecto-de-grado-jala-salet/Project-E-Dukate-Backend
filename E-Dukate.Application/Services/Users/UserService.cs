using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Application.DTOs.Common;
using FuzzySharp; // Importar FuzzySharp

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
        
        var roles = new[]
        {
            new { Spanish = "especialista", English = "specialist" },
            new { Spanish = "administrador", English = "administrator" }
        };

        string? matchedRole = null;
        int similarityThreshold = 50;
        foreach (var role in roles)
        {
            int spanishSimilarity = Fuzz.Ratio(searchTerm, role.Spanish);
            int englishSimilarity = Fuzz.Ratio(searchTerm, role.English);
            if (spanishSimilarity >= similarityThreshold || englishSimilarity >= similarityThreshold)
            {
                matchedRole = role.English;
                break;
            }
        }

        var admins = _adminRepository.GetAll()
            .Select(a => new UserDto
            {
                Id = a.Id,
                Names = a.Names,
                LastNamePaternal = a.LastNamePaternal,
                LastNameMaternal = a.LastNameMaternal ?? string.Empty,
                MobileNumber = a.MobileNumber,
                Role = "Administrator"
            })
            .Where(a =>
                a.Names.ToLower().Contains(searchTerm) ||
                a.LastNamePaternal.ToLower().Contains(searchTerm) ||
                (a.LastNameMaternal != null && a.LastNameMaternal.ToLower().Contains(searchTerm)) ||
                a.MobileNumber.Contains(searchTerm) ||
                (matchedRole == "administrator" && a.Role.ToLower() == "administrator")
            );
        
        var specialists = _specialistRepository.GetAll()
            .Select(s => new UserDto
            {
                Id = s.Id,
                Names = s.Names,
                LastNamePaternal = s.LastNamePaternal,
                LastNameMaternal = s.LastNameMaternal ?? string.Empty,
                MobileNumber = s.MobileNumber,
                Role = "Specialist"
            })
            .Where(s =>
                s.Names.ToLower().Contains(searchTerm) ||
                s.LastNamePaternal.ToLower().Contains(searchTerm) ||
                (s.LastNameMaternal != null && s.LastNameMaternal.ToLower().Contains(searchTerm)) ||
                s.MobileNumber.Contains(searchTerm) ||
                (matchedRole == "specialist" && s.Role.ToLower() == "specialist")
            );

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