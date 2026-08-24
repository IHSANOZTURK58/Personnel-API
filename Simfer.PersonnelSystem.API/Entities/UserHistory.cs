namespace Simfer.PersonnelSystem.API.Entities
{
    public class UserHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ActionType { get; set; }
        public string Details { get; set; }
        public DateTime ActionDate { get; set; } = DateTime.Now;
    }
}