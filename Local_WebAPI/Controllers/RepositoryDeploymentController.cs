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

        //private readonly ILogger<DeploymentController> _logger;

        //public DeploymentController(ILogger<DeploymentController> logger)
        //{
        //    _logger = logger;
        //}

        // POST: api/Deployment
        [HttpPost]
        public IActionResult PostDeployment([FromBody] DeploymentModel request)
        {
            // TODO: what user will this run under in iis01? Will need to make sure it has access to DB
            // TODO: want logging in some way, probably write to a new table in HuntHome

            if (request == null || string.IsNullOrWhiteSpace(request.Repository))
            {
                return BadRequest("The parameter is required.");
            }

#if DEBUG
            string? connectionString = Environment.GetEnvironmentVariable("ConnectionStringDebug");
            var logMethod = modLogging.eLogMethod.CONSOLE;
#else
            string? connectionString = Environment.GetEnvironmentVariable("ConnectionStringRelease");
            var logMethod = modLogging.eLogMethod.DATABASE;
#endif
            if (connectionString == null)
            {
                modLogging.AddLog(Program.programName, "C#", "DeploymentController.PostDeployment", modLogging.eLogLevel.CRITICAL, "Unable to read connection string", logMethod);
                Environment.Exit(-1);
            }

            var command = new SqlCommand();
            command.Connection = modDatabase.Connection(connectionString);
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "SELECT RepoPath FROM HuntHome.dbo.Repositories WHERE RepoName = @RepoName";
            command.Parameters.AddWithValue("@RepoName", request.Repository);
            
            var rtnval = command.ExecuteScalar();
            if (rtnval == null)
            {
                // TODO: no record for the repo name, return 404 with message "Repository 'Name' does not exist"
            }
            else if (rtnval == DBNull.Value)
            {
                // TODO: return 404 with message "Repository 'Name' deployment path does not exist"
            }
            else
            {
                string repoPath = rtnval.ToString()!;
                if (!Path.Exists(repoPath))
                {
                    // TODO: return 404 with message "Repository 'Name' deployment path does not exist"
                }

                try
                {
                    System.IO.File.Create(Path.Combine(repoPath, "deploy.txt"));
                    // TODO: return 200 with message "Respository 'Name' queued for deployment"
                }
                catch
                {
                    // TODO: return 500 with message "Failed to create deploy.txt for repository 'name'"
                }
            }

            return Ok(new
            {
                Message = "Deployment started successfully.",
                ReceivedParameter = request.Repository
            });
        }
    }
}
