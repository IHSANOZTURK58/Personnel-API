namespace Simfer.PersonnelSystem.API.DTOs
{
    public class FaultyProductResolveDto
    {
        public int Id { get; set; }
        public string ResolutionDetails { get; set; } = string.Empty;
        public string? ResolvedByName { get; set; }
    }
}   