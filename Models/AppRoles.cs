namespace IsanDer.Models;

public static class AppRoles
{
    public const string Customer = "Customer";
    public const string Staff = "Staff";
    public const string Accountant = "Accountant";
    public const string Manager = "Manager";
    public const string Admin = "Admin";

    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>
        {
            [Customer] = "ลูกค้า",
            [Staff] = "พนักงาน",
            [Accountant] = "พนักงานบัญชี",
            [Manager] = "Manager",
            [Admin] = "แอดมิน"
        };

    public static readonly IReadOnlyDictionary<string, string> AssignableLabels =
        new Dictionary<string, string>
        {
            [Customer] = Labels[Customer],
            [Staff] = Labels[Staff],
            [Accountant] = Labels[Accountant],
            [Manager] = Labels[Manager],
            [Admin] = Labels[Admin]
        };

    public static string? NormalizeRole(string? role) =>
        role switch
        {
            "Receptionist" or "Chef" or "StockChecker" => Staff,
            _ => role
        };

    public static bool IsValid(string? role)
    {
        var normalizedRole = NormalizeRole(role);
        return normalizedRole is not null && Labels.ContainsKey(normalizedRole);
    }
}
