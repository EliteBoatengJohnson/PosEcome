using PosSystem.SharedKernel;
namespace PosSystem.Modules.Branches.Entities;


public class Branch : BaseEntity
{
    public string Name {get; set;} = default!;
    public string Code {get; set;} = default!;

    public string? Address {get; set;}
    public string? Phone {get; set;}
    public string Email {get; set;}
    public string ManagerName {get; set;}
    public  bool IsActive {get; set;} = true;
    public bool IsHeadOffice {get; set;}


} 