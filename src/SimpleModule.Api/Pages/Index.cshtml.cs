using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SimpleModule.Api.Pages;

public class IndexModel(IWebHostEnvironment env) : PageModel
{
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    public string DisplayName => User.Identity?.Name ?? "User";
    public bool IsDevelopment => env.IsDevelopment();

    public void OnGet() { }
}
