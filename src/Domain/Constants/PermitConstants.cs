namespace SAPFIAI.Domain.Constants;

public static class PermitConstants
{
    public static class Resources
    {
        public const string ManageUsers = "manage_users";
    }

    public static class Actions
    {
        public const string Create = "create";
        public const string Read = "read";
        public const string Update = "update";
        public const string Delete = "delete";
        public const string List = "list";
        public const string AssignRole = "assign_role";
        public const string RemoveRole = "remove_role";
        public const string AssignPermission = "assign_permission";
        public const string RemovePermission = "remove_permission";
    }

    public static class Roles
    {
        public const string Admin = "admin";
        public const string Manager = "manager";
        public const string User = "user";
    }

    public static class Tenants
    {
        public const string Default = "default";
    }

    public static readonly string[] AssignableRoles =
    [
        Roles.Admin,
        Roles.Manager,
        Roles.User
    ];

    public static readonly string[] ManageUsersActions =
    [
        Actions.Create,
        Actions.Read,
        Actions.Update,
        Actions.Delete,
        Actions.List,
        Actions.AssignRole,
        Actions.RemoveRole,
        Actions.AssignPermission,
        Actions.RemovePermission
    ];
}