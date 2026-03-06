namespace Pheidi.Common.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsPaidUser { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
