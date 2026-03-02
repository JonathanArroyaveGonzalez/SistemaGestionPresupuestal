namespace SAPFIAI.Application.Common.Models;

public class IpBlackListDto
{
    public int Id { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
