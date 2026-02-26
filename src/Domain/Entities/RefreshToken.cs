namespace SAPFIAI.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; private set; }
    public string UserId { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedDate { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReasonRevoked { get; private set; }

    // Computed properties
    public bool IsActive => !IsRevoked && !IsExpired;
    public bool IsExpired => DateTime.UtcNow >= ExpiryDate;

    private RefreshToken(string token, string userId, DateTime expiryDate, string? createdByIp)
    {
        Token = token;
        UserId = userId;
        ExpiryDate = expiryDate;
        CreatedDate = DateTime.UtcNow;
        CreatedByIp = createdByIp;
    }

    private RefreshToken() 
    {
        Token = string.Empty;
        UserId = string.Empty;
    }

    public static RefreshToken Create(string token, string userId, TimeSpan expiresIn, string? createdByIp)
    {
        return new RefreshToken(token, userId, DateTime.UtcNow.Add(expiresIn), createdByIp);
    }

    public void Revoke(string ipAddress, string reason, string? replacedByToken = null)
    {
        IsRevoked = true;
        RevokedDate = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        ReasonRevoked = reason;
        ReplacedByToken = replacedByToken;
    }
}
