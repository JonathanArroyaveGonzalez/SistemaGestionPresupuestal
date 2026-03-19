namespace SAPFIAI.Application.Common.Models;

public record PermitRoleDto(string Key, string Name, IEnumerable<string> Permissions);

public record PermitResourceDto(string Key, string Name, IEnumerable<string> Actions);
