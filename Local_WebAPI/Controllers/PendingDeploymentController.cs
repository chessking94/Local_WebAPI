using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Utilities_NetCore;

namespace Local_WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PendingDeploymentController : ControllerBase
    {
        // GET: /PendingDeployment
        [HttpGet]
        public IActionResult Get_PendingDeployment()
        {
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

            List<string> repositories = new List<string>();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "SELECT RepoName FROM HuntHome.dev.Repositories WHERE DeploymentQueued = 1";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    repositories.Add(reader.GetString(0));
                }
            }
            repositories.Sort();

            // return the list, even if it is empty
            return Ok(new
            {
                Repositories = repositories
            });
        }
    }
}
