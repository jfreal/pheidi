namespace Pheidi.Common.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsPaidUser { get; set; }
    public string ShareToken { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
