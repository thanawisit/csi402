namespace IsanDer.Models;

public static class OrderStatusLabels
{
    public static string Display(string? status) => status switch
    {
        "รอดำเนินการ" => "รอร้านยืนยัน",
        "เสร็จแล้ว" => "เสร็จสิ้น",
        _ => status ?? string.Empty
    };
}
