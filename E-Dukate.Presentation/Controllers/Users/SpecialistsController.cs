using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Users;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Application.DTOs.Common;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Auth;
using E_Dukate.Domain.Interfaces;
using FluentValidation;

namespace E_Dukate.Presentation.Controllers.Users;

public class SpecialistsController : BaseController<Specialist, SpecialistDto>
{
    private readonly SpecialistService _specialistService;
    private readonly IGenericRepository<UserAuth> _userAuthRepository;

    public SpecialistsController(
        SpecialistService service,
        IGenericRepository<UserAuth> userAuthRepository)
        : base(service)
    {
        _specialistService = service;
        _userAuthRepository = userAuthRepository;
    }

    [HttpPost]
    public override IActionResult Add([FromBody] SpecialistDto dto)
    {
        try
        {
            var result = _specialistService.Register(dto);
            if (!result.IsSuccess)
            {
                return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });
            }

            return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
        }
        catch (Exception ex) when (ex.Message == "The chosen specialty does not exist")
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public override IActionResult Update(Guid id, [FromBody] SpecialistDto dto)
    {
        var result = _specialistService.Update(id, dto);
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage == "The chosen specialty does not exist")
                return BadRequest(new { Error = result.ErrorMessage });
            if (result.ErrorMessage == "Specialist not found.")
                return NotFound(new { Error = result.ErrorMessage });
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });
        }

        return NoContent();
    }

    [HttpGet("{id}")]
    public override IActionResult GetById(Guid id)
    {
        var specialist = _specialistService.GetSpecialistById(id);
        if (specialist == null) return NotFound();

        var userAuth = _userAuthRepository.GetAll()
            .FirstOrDefault(u => u.UserId == id && u.UserRole == "Specialist");

        var response = new
        {
            specialist.Id,
            specialist.Names,
            specialist.LastNamePaternal,
            specialist.LastNameMaternal,
            specialist.MobileNumber,
            specialist.IdentityCard,
            specialist.PhoneNumber,
            specialist.Age,
            specialist.Gender,
            specialist.DateOfBirth,
            specialist.Address,
            Email = userAuth?.Email ?? string.Empty,
            Specialty = specialist.Specialty?.TypeOfSpecialty,
            specialist.YearsOfExperience,
            specialist.SpecialistCode,
            Schedules = specialist.Schedules.Select(s => new
            {
                s.Id,
                DayOfWeek = s.DayOfWeek.ToString(),
                Attends = s.Attends,
                TimeSlots = s.TimeSlots.Select(ts => new
                {
                    ts.Id,
                    StartTime = ts.StartTime.ToString("HH:mm"),
                    EndTime = ts.EndTime.ToString("HH:mm")
                }).ToList()
            }).ToList()
        };
        return Ok(response);
    }

    [HttpGet]
    public override async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var specialists = _specialistService.GetAllSpecialists()
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToList();
        var totalCount = _specialistService.GetAllSpecialists().Count();

        var userAuths = _userAuthRepository.GetAll()
            .Where(u => u.UserRole == "Specialist")
            .ToDictionary(u => u.UserId, u => u.Email);

        var response = specialists.Select(specialist => new
        {
            specialist.Id,
            specialist.Names,
            specialist.LastNamePaternal,
            specialist.LastNameMaternal,
            specialist.MobileNumber,
            specialist.IdentityCard,
            specialist.PhoneNumber,
            specialist.Age,
            specialist.Gender,
            specialist.DateOfBirth,
            specialist.Address,
            Email = userAuths.ContainsKey(specialist.Id) ? userAuths[specialist.Id] : string.Empty,
            Specialty = specialist.Specialty?.TypeOfSpecialty,
            specialist.YearsOfExperience,
            specialist.SpecialistCode,
            Schedules = specialist.Schedules.Select(s => new
            {
                s.Id,
                DayOfWeek = s.DayOfWeek.ToString(),
                Attends = s.Attends,
                TimeSlots = s.TimeSlots.Select(ts => new
                {
                    ts.Id,
                    StartTime = ts.StartTime.ToString("HH:mm"),
                    EndTime = ts.EndTime.ToString("HH:mm")
                }).ToList()
            }).ToList()
        });

        return Ok(new
        {
            Items = response,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
        });
    }
}