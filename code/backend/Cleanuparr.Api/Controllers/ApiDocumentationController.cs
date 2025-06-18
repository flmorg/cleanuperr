using Microsoft.AspNetCore.Mvc;

namespace Cleanuparr.Api.Controllers;

[ApiController]
[Route("")]
public class ApiDocumentationController : ControllerBase
{
    [HttpGet]
    public IActionResult RedirectToSwagger()
    {
        return Redirect("/swagger");
    }
}
