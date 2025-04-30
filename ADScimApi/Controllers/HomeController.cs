using Microsoft.AspNetCore.Mvc;

namespace ADScimApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Active Directory SCIM API is running", version = "1.0.0" });
    }
}