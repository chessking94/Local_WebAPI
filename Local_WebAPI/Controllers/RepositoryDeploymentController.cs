using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Utilities_NetCore;

namespace Local_WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeploymentController : ControllerBase
    {
        public class DeploymentModel
        {
            public required string Repository { get; set; }
        }

        // POST: /Deployment
        [HttpPost]
        public IActionResult Post_Deployment([FromBody] DeploymentModel request)
        {
            // TODO: want logging in some way, probably write to a new table in database
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Repository))
                {
                    return BadRequest("The parameter is required.");
                }

#if DEBUG
                string? connectionString = Environment.GetEnvironmentVariable("ConnectionStringDebug");
#else
                string? connectionString = Environment.GetEnvironmentVariable("ConnectionStringRelease");
#endif
                var command = new SqlCommand();
                try
                {
                    if (connectionString == null)
                    {
                        return BadRequest("database connection not defined");
                    }

                    command.Connection = modDatabase.Connection(connectionString);
                }
                catch
                {
                    return BadRequest("unable to connect to the database");
                }

                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "SELECT DeploymentQueued FROM HuntHome.dev.Repositories WHERE RepoName = @RepoName";
                command.Parameters.AddWithValue("@RepoName", request.Repository);

                var rtnval = command.ExecuteScalar();
                if (rtnval == null)
                {
                    // no record found in database
                    return NotFound($"repository '{request.Repository}' does not exist");
                }
                else
                {
                    try
                    {
                        if (Convert.ToInt16(rtnval) == 0)
                        {
                            command.CommandText = "UPDATE HuntHome.dev.Repositories SET DeploymentQueued = 1 WHERE RepoName = @RepoName";
                            command.ExecuteNonQuery();
                        }

                        // succcessful request
                        return Ok(new
                        {
                            Message = "queued for deployment",
                            ReceivedParameter = request.Repository
                        });
                    }
                    catch (Exception ex)
                    {
                        // unable to update the database for some reason
                        return Problem(detail: $"failed to queue deployment: {ex.Message}", statusCode: 500);
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"catastrophic error: {ex.Message}");
            }
        }
    }
}
