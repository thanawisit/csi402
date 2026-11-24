using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsanDer.Models;

[Table("table_reservations")]
public sealed class TableReservation
{
    [Key]
    public int ReservationID { get; set; }

    public int UserID { get; set; }

    public int TableID { get; set; }

    public string Package { get; set; } = null!;

    public DateTime ReservationDateTime { get; set; }

    public DateTime ReservationEndTime { get; set; }

    public int GuestCount { get; set; }

    public string CustomerName { get; set; } = null!;

    public string CustomerEmail { get; set; } = null!;

    public string CustomerPhone { get; set; } = null!;

    public string? Note { get; set; }

    public string Status { get; set; } = ReservationStatuses.Pending;

    public DateTime CreatedAt { get; set; }
}

public static class ReservationStatuses
{
    public const string Pending = "รอยืนยัน";
    public const string Confirmed = "ยืนยันแล้ว";
    public const string Cancelled = "ยกเลิก";
    public const string Completed = "เสร็จสิ้น";

    public static IReadOnlyList<string> Values { get; } =
    [
        Pending,
        Confirmed,
        Cancelled,
        Completed
    ];

    public static bool IsValid(string? status) =>
        status is not null && Values.Contains(status, StringComparer.Ordinal);
}
