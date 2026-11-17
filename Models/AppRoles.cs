namespace IsanDer.Models;

public static class AppRoles
{
    public const string Customer = "Customer";
    public const string Receptionist = "Receptionist";
    public const string Chef = "Chef";
    public const string Manager = "Manager";
    public const string StockChecker = "StockChecker";
    public const string Admin = "Admin";

    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>
        {
            [Customer] = "ลูกค้า",
            [Receptionist] = "พนักงานต้อนรับ",
            [Chef] = "พ่อครัว",
            [Manager] = "Manager",
            [StockChecker] = "คนเช็คสต๊อก",
            [Admin] = "แอดมิน"
        };

    public static bool IsValid(string? role) =>
        role is not null && Labels.ContainsKey(role);
}
