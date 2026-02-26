namespace SAPFIAI.Domain.Entities;

public class LoginAttempt : BaseEntity
{
    public string Email { get; private set; }
    public string IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime AttemptDate { get; private set; }
    public bool WasSuccessful { get; private set; }
    public string? FailureReason { get; private set; }
    public LoginFailureReason? FailureReasonType { get; private set; }

    private LoginAttempt(string email, string ipAddress, string? userAgent, bool wasSuccessful, string? failureReason, LoginFailureReason? failureReasonType)
    {
        Email = email;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        AttemptDate = DateTime.UtcNow;
        WasSuccessful = wasSuccessful;
        FailureReason = failureReason;
        FailureReasonType = failureReasonType;
    }

    private LoginAttempt() 
    {
        Email = string.Empty;
        IpAddress = string.Empty;
    }

    public static LoginAttempt RecordSuccess(string email, string ipAddress, string? userAgent)
    {
        return new LoginAttempt(email, ipAddress, userAgent, true, null, null);
    }

    public static LoginAttempt RecordFailure(string email, string ipAddress, string? userAgent, string failureReason, LoginFailureReason failureReasonType)
    {
        return new LoginAttempt(email, ipAddress, userAgent, false, failureReason, failureReasonType);
    }
}
