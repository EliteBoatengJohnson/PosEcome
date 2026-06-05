namespace PosSystem.SharedKernel;

public  abstract class BaseEntity
{
   public Guid Id { get; init; } = new Guid();
   public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
   public DateTime? UpdatedAt { get; set; }
   public Guid? CreatedBy { get; set; }
   public bool IsDeleted { get; set; } = false;
}
