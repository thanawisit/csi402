using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace IsanDer.Controllers;

public class CartCountViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(0);
        }

        var email = HttpContext.User.Claims
            .FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("The authenticated user has no email claim.");
        }

        var cartJson = HttpContext.Session.GetString($"Cart:{email}");
        var cart = cartJson == null
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

        return View(cart.Sum(item => item.Qty));
    }
}
