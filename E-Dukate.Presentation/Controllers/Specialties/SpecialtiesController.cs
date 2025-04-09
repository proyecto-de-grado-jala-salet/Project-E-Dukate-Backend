using E_Dukate.Application.Services.Specialties;
using E_Dukate.Application.DTOs.Specialties;
using E_Dukate.Domain.Entities.Specialties;

namespace E_Dukate.Presentation.Controllers.Specialties;

public class SpecialtiesController : BaseController<Specialty, SpecialtyDto>
{
    public SpecialtiesController(SpecialtyService service) : base(service) { }
}