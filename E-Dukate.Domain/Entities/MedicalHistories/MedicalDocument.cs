using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.MedicalHistories;

public class MedicalDocument : Entity
{
    public Guid PermissionId { get; set; }
    public MedicalHistoryPermission? Permission { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public DateTime UploadDate { get; set; }
}