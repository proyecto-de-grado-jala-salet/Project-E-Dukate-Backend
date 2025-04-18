using E_Dukate.Application.Services.Users;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Presentation.Controllers.Users;

public class AdministratorsController : BaseController<Administrator, AdministratorDto>
{
    public AdministratorsController(AdministratorService service) : base(service) { }
}