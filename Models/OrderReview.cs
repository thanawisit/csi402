using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsanDer.Models;

[Table("order_reviews")]
public class OrderReview
{
    [Key]
    public int ReviewId { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsHidden { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? User { get; set; }
}
