using E_Dukate.Application.Services.Users;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Presentation.Controllers.Users;

public class PatientsController : BaseController<Patient, PatientDto>
{
    public PatientsController(PatientService service) : base(service) { }
}