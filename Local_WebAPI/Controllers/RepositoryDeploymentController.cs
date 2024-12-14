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

        // POST: api/Deployment
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
                        // modLogging.AddLog(Program.programName, "C#", "DeploymentController.Post_Deployment", modLogging.eLogLevel.CRITICAL, "Unable to read connection string", logMethod);
                        return BadRequest("database connection not defined");
                    }

                    command.Connection = modDatabase.Connection(connectionString);
                }
                catch
                {
                    return BadRequest("unable to connect to the database");
                }

                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "SELECT RepoPath FROM HuntHome.dbo.Repositories WHERE RepoName = @RepoName";
                command.Parameters.AddWithValue("@RepoName", request.Repository);

                var rtnval = command.ExecuteScalar();
                if (rtnval == null)
                {
                    // no record found in database
                    return NotFound($"repository '{request.Repository}' does not exist");
                }
                else if (rtnval == DBNull.Value)
                {
                    // record found, no project path
                    return NotFound("undefined deployment path");
                }
                else
                {
                    string repoPath = rtnval.ToString()!;
                    if (!Path.Exists(repoPath))
                    {
                        // project path does not exist
                        return NotFound($"deployment path '{repoPath}' does not exist");
                    }

                    try
                    {
                        using (var fileStream = System.IO.File.Create(Path.Combine(repoPath, "deploy.txt")))
                        {
                            // create an empty file, use 'using' to ensure the file lock is released
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
                        // unable to create the deploy.txt for some reason
                        return Problem(detail: $"failed to create 'deploy.txt': {ex.Message}", statusCode: 500);
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
