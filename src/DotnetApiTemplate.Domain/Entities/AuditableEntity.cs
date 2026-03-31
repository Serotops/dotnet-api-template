namespace DotnetApiTemplate.Domain.Entities;

public class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? ModifiedBy { get; set; }
}
