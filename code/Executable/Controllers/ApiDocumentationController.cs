using Microsoft.AspNetCore.Mvc;

namespace Executable.Controllers;

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
