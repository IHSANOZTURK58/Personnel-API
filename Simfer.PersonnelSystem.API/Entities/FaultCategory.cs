using Simfer.PersonnelSystem.API.Entities;

public class FaultCategory
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<FaultyProduct> FaultyProducts { get; set; }
    public int? ResolvedByUserId { get; set; }

    public virtual User ResolvedByUser { get; set; }
}