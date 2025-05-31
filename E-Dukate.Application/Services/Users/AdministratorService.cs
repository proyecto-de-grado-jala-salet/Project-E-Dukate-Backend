using E_Dukate.Domain.Interfaces;
using FluentValidation;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Auth;
using E_Dukate.Application.Services.Auth;
using E_Dukate.Domain.Primitives;
using E_Dukate.Application.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Application.Services.Users;

public class AdministratorService : BaseService<Administrator, AdministratorDto>
{
    private readonly IGenericRepository<UserAuth> _userAuthRepository;
    private readonly AuthService _authService;

    public AdministratorService(
        IGenericRepository<Administrator> repository,
        IGenericRepository<UserAuth> userAuthRepository,
        IValidator<AdministratorDto> validator,
        AuthService authService)
        : base(repository, validator)
    {
        _userAuthRepository = userAuthRepository;
        _authService = authService;
    }

    public override Result Register(AdministratorDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var existingAuth = _userAuthRepository.GetAll().FirstOrDefault(u => u.Email == dto.Email);
        if (existingAuth != null)
            return Result.Failure("El correo ya está registrado.");

        var admin = MapToEntity(dto);
        Repository.Add(admin);

        var userAuth = new UserAuth
        {
            UserId = admin.Id,
            UserRole = "Administrator",
            Email = dto.Email,
            PasswordHash = _authService.HashPassword(dto.Password)
        };
        _userAuthRepository.Add(userAuth);

        return Result.Success();
    }

    public override Result Update(Guid id, AdministratorDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var existing = Repository.GetById(id);
        if (existing == null)
            return Result.Failure("Administrador no encontrado.");

        var existingAuth = _userAuthRepository.GetAll().FirstOrDefault(u => u.UserId == id && u.UserRole == "Administrator");
        if (existingAuth == null)
            return Result.Failure("Registro de autenticación no encontrado.");

        var duplicateAuth = _userAuthRepository.GetAll().FirstOrDefault(u => u.Email == dto.Email && u.Id != existingAuth.Id);
        if (duplicateAuth != null)
            return Result.Failure("El correo ya está registrado por otro usuario.");

        UpdateEntity(existing, dto);
        Repository.Update(existing);

        existingAuth.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.Password))
            existingAuth.PasswordHash = _authService.HashPassword(dto.Password);
        _userAuthRepository.Update(existingAuth);

        return Result.Success();
    }

    protected override Administrator MapToEntity(AdministratorDto dto)
    {
        return new Administrator
        {
            Names = dto.Names,
            LastNamePaternal = dto.LastNamePaternal,
            LastNameMaternal = dto.LastNameMaternal,
            MobileNumber = dto.MobileNumber,
            IdentityCard = dto.IdentityCard,
            PhoneNumber = dto.PhoneNumber,
            Age = dto.Age,
            Gender = dto.Gender,
            DateOfBirth = dto.DateOfBirth,
            Address = dto.Address
        };
    }

    protected override void UpdateEntity(Administrator entity, AdministratorDto dto)
    {
        entity.Names = dto.Names;
        entity.LastNamePaternal = dto.LastNamePaternal;
        entity.LastNameMaternal = dto.LastNameMaternal;
        entity.MobileNumber = dto.MobileNumber;
        entity.IdentityCard = dto.IdentityCard;
        entity.PhoneNumber = dto.PhoneNumber;
        entity.Age = dto.Age;
        entity.Gender = dto.Gender;
        entity.DateOfBirth = dto.DateOfBirth;
        entity.Address = dto.Address;
    }

    public async Task<(IEnumerable<Administrator> Items, int TotalCount)> SearchAdministratorsAsync(string searchTerm, PaginationParams pagination)
    {
        var query = Repository.GetAll();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(a =>
                a.Names.ToLower().Contains(searchTerm) ||
                a.LastNamePaternal.ToLower().Contains(searchTerm) ||
                (a.LastNameMaternal != null && a.LastNameMaternal.ToLower().Contains(searchTerm)) ||
                a.MobileNumber.Contains(searchTerm) ||
                a.IdentityCard.ToString().Contains(searchTerm) ||
                a.Age.ToString().Contains(searchTerm) ||
                a.Gender.ToLower().Contains(searchTerm) ||
                _userAuthRepository.GetAll().Any(u => u.UserId == a.Id && u.Email.ToLower().Contains(searchTerm))
            );
        }
        
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Names)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}