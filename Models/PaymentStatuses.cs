namespace IsanDer.Models;

public static class PaymentStatuses
{
    public const string Unpaid = "รอชำระเงิน";
    public const string PendingReview = "รอตรวจสอบ";
    public const string NeedsResubmission = "กรุณาส่งสลิปใหม่";
    public const string Paid = "ชำระแล้ว";
}
