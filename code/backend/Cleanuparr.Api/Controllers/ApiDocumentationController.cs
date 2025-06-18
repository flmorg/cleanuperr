using Microsoft.AspNetCore.Mvc;

namespace Cleanuparr.Api.Controllers;

[ApiController]
[Route("api")]
public class ApiDocumentationController : ControllerBase
{
    [HttpGet]
    public IActionResult RedirectToSwagger()
    {
        return Redirect("/api/swagger");
    }
}
