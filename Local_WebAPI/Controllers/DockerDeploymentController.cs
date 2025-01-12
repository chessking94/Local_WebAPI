using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;

namespace Local_WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DockerDeploymentController : ControllerBase
    {
        // POST: api/DockerDeployment
        [HttpPost]
        public IActionResult Post_DockerDeployment()
        {
            try
            {
                string? docker_host = Environment.GetEnvironmentVariable("DockerHost");
                string? docker_user = Environment.GetEnvironmentVariable("DockerUser");
                string? docker_pwd = Environment.GetEnvironmentVariable("DockerPassword");

                if (docker_host == null)
                {
                    return BadRequest("docker host not defined");
                }

                if (docker_user == null)
                {
                    return BadRequest("docker user not defined");
                }


                if (docker_pwd == null)
                {
                    return BadRequest("docker password not defined");
                }

                using (var client = new SshClient(docker_host!, docker_user!, docker_pwd!))
                {
                    client.Connect();

                    if (!client.IsConnected)
                    {
                        return Problem(detail: "failed to ssh connect to the server", statusCode: 500);
                    }

                    try
                    {
                        using (var shell = client.CreateShellStream("bash", 80, 24, 800, 600, 1024))
                        {
                            // need to restore the directory first in case permissions changed
                            string ssh_command = "cd /srv/docker && sudo git restore .";
                            shell.WriteLine(ssh_command);
                            shell.WriteLine(docker_pwd);

                            ssh_command = "cd /srv/docker && sudo git pull origin master";
                            shell.WriteLine(ssh_command);
                            shell.WriteLine(docker_pwd);

                            var output = new System.Text.StringBuilder();
                            string line;
                            while ((line = shell.ReadLine(TimeSpan.FromSeconds(5))!) != null)
                            {
                                output.AppendLine(line);
                            }

                            ssh_command = "sudo -S bash /srv/docker/restart_stacks.sh";
                            shell.WriteLine(ssh_command);
                            shell.WriteLine(docker_pwd);

                            output = new System.Text.StringBuilder();
                            while ((line = shell.ReadLine(TimeSpan.FromSeconds(5))!) != null)
                            {
                                output.AppendLine(line);
                            }

                            client.Disconnect();

                            return Ok(new
                            {
                                Message = "containers successfully deployed"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Problem(detail: $"failed to deploy containers: {ex.Message}", statusCode: 500);
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
