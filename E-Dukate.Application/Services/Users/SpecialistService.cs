using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Users;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Entities.Auth;
using E_Dukate.Application.Services.Auth;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Application.Services.Users;

public class SpecialistService : BaseService<Specialist, SpecialistDto>
{
    private readonly IGenericRepository<Specialty> _specialtyRepository;
    private readonly IGenericRepository<UserAuth> _userAuthRepository;
    private readonly AuthService _authService;

    public SpecialistService(
        IGenericRepository<Specialist> repository,
        IGenericRepository<Specialty> specialtyRepository,
        IGenericRepository<UserAuth> userAuthRepository,
        IValidator<SpecialistDto> validator,
        AuthService authService)
        : base(repository, validator)
    {
        _specialtyRepository = specialtyRepository;
        _userAuthRepository = userAuthRepository;
        _authService = authService;
    }

    public override Result Register(SpecialistDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var existingAuth = _userAuthRepository.GetAll().FirstOrDefault(u => u.Email == dto.Email);
        if (existingAuth != null)
            return Result.Failure("El correo ya está registrado.");

        var specialist = MapToEntity(dto);
        Repository.Add(specialist);

        var userAuth = new UserAuth
        {
            UserId = specialist.Id,
            UserRole = "Specialist",
            Email = dto.Email,
            PasswordHash = _authService.HashPassword(dto.Password)
        };
        _userAuthRepository.Add(userAuth);

        return Result.Success();
    }

    public override Result Update(Guid id, SpecialistDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var existing = Repository.GetById(id);
        if (existing == null)
            return Result.Failure("Especialista no encontrado.");

        var existingAuth = _userAuthRepository.GetAll().FirstOrDefault(u => u.UserId == id && u.UserRole == "Specialist");
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

    public Specialist? GetSpecialistById(Guid id) =>
        Repository.GetAll()
            .Include(s => s.Specialty)
            .Include(s => s.Schedules)
            .FirstOrDefault(s => s.Id == id);

    public IEnumerable<Specialist> GetAllSpecialists() =>
        Repository.GetAll()
            .Include(s => s.Specialty)
            .Include(s => s.Schedules)
            .ToList();

    protected override Specialist MapToEntity(SpecialistDto dto)
    {
        var specialty = _specialtyRepository.GetAll()
            .FirstOrDefault(s => s.TypeOfSpecialty == dto.TypeOfSpecialty)
            ?? throw new Exception("The chosen specialty does not exist");

        return new Specialist
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
            Address = dto.Address,
            Specialty = specialty,
            YearsOfExperience = dto.YearsOfExperience,
            SpecialistCode = dto.SpecialistCode
        };
    }

    protected override void UpdateEntity(Specialist entity, SpecialistDto dto)
    {
        var specialty = _specialtyRepository.GetAll()
            .FirstOrDefault(s => s.TypeOfSpecialty == dto.TypeOfSpecialty)
            ?? throw new Exception("The chosen specialty does not exist");

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
        entity.Specialty = specialty;
        entity.YearsOfExperience = dto.YearsOfExperience;
        entity.SpecialistCode = dto.SpecialistCode;
    }
}