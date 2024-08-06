using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sso.Pages.Error;

[AllowAnonymous]
public class StatusPage : PageModel
{
    public int Status { get; set; }
    
    public void OnGet(int status)
    {
        Status = status;
    }
}