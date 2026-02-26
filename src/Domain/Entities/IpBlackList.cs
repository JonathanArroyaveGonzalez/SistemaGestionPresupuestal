namespace SAPFIAI.Domain.Entities;

public class IpBlackList : BaseEntity
{
    public string IpAddress { get; private set; }
    public string Reason { get; private set; }
    public DateTime BlockedDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public string? BlockedBy { get; private set; }
    public string? Notes { get; private set; }
    public BlackListReason BlackListReason { get; private set; }
    
    public bool IsActive => ExpiryDate == null || DateTime.UtcNow < ExpiryDate;

    private IpBlackList(string ipAddress, string reason, DateTime blockedDate, DateTime? expiryDate, string? blockedBy, string? notes, BlackListReason blackListReason)
    {
        IpAddress = ipAddress;
        Reason = reason;
        BlockedDate = blockedDate;
        ExpiryDate = expiryDate;
        BlockedBy = blockedBy;
        Notes = notes;
        BlackListReason = blackListReason;
    }

    // Required by EF Core
    private IpBlackList() 
    {
        IpAddress = string.Empty;
        Reason = string.Empty;
    }

    public static IpBlackList Block(string ipAddress, string reason, DateTime? expiryDate, string? blockedBy, string? notes, BlackListReason blackListReason)
    {
        return new IpBlackList(ipAddress, reason, DateTime.UtcNow, expiryDate, blockedBy, notes, blackListReason);
    }

    public void Unblock()
    {
        ExpiryDate = DateTime.UtcNow;
    }
}
