using System;
using System.Collections.Generic;

namespace IsanDer.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public bool MarketingConsent { get; set; }

    public DateTime? TermsAcceptedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
