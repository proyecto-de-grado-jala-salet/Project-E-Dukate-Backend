using E_Dukate.Application.Services.Users;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Application.DTOs.Common;
using E_Dukate.Domain.Entities.Auth;
using E_Dukate.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace E_Dukate.Presentation.Controllers.Users;

public class AdministratorsController : BaseController<Administrator, AdministratorDto>
{
    private readonly AdministratorService _administratorService;
    private readonly IGenericRepository<UserAuth> _userAuthRepository;

    public AdministratorsController(
        AdministratorService service,
        IGenericRepository<UserAuth> userAuthRepository) : base(service)
    {
        _administratorService = service;
        _userAuthRepository = userAuthRepository;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string searchTerm, [FromQuery] PaginationParams pagination)
    {
        var (items, totalCount) = await _administratorService.SearchAdministratorsAsync(searchTerm, pagination);
        if (!items.Any())
        {
            return NotFound(new { Message = "No se encontraron resultados de lo buscado" });
        }

        var userAuths = _userAuthRepository.GetAll()
            .Where(u => u.UserRole == "Administrator")
            .ToDictionary(u => u.UserId, u => u.Email);

        var response = items.Select(admin => new
        {
            admin.Id,
            admin.Names,
            admin.LastNamePaternal,
            admin.LastNameMaternal,
            admin.MobileNumber,
            admin.IdentityCard,
            admin.PhoneNumber,
            admin.Age,
            admin.Gender,
            admin.DateOfBirth,
            admin.Address,
            Email = userAuths.ContainsKey(admin.Id) ? userAuths[admin.Id] : string.Empty
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