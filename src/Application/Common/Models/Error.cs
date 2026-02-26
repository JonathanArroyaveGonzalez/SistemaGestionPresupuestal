namespace SAPFIAI.Application.Common.Models;

public record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");
    
    // Default errors
    public static Error Failure(string code, string description) => new(code, description);
    public static Error NotFound(string code, string description) => new(code, description);
    public static Error Problem(string code, string description) => new(code, description);
    public static Error Conflict(string code, string description) => new(code, description);
}
