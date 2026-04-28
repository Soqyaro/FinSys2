namespace FinSys2.Models
{
    public class Transaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }
}
