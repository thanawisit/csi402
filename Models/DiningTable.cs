using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsanDer.Models;

[Table("dining_tables")]
public class DiningTable
{
    [Key]
    public int TableID { get; set; }

    [Required]
    [MaxLength(10)]
    public string TableNumber { get; set; } = null!;

    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
