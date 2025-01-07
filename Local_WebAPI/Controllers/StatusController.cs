using Microsoft.AspNetCore.Mvc;

namespace Local_WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        // GET: api/Status
        [HttpGet]
        public IActionResult Get_Status()
        {
            return Ok(new
            {
                Message = "API is active"
            });
        }
    }
}
