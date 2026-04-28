namespace FinSys2.Models
{
    public class Goal
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string Title { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal AllocatedPercentage { get; set; }
        public bool IsCompleted { get; set; } = false;
    }
}
