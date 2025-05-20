using E_Dukate.Application.DTOs.MedicalHistories;
using FluentValidation;

namespace E_Dukate.Application.Validators.MedicalHistories;

public class UpdateMedicalConsultationDtoValidator : AbstractValidator<UpdateMedicalConsultationDto>
{
    public UpdateMedicalConsultationDtoValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required.");
        RuleFor(x => x.ConsultationDate).NotEmpty().WithMessage("Consultation date is required.");
    }
}
