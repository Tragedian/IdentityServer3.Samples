using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Configuration;
using IdentityServer3.Core.Configuration;
using Serilog;
using System.IO;

[assembly: OwinStartup(typeof(WebHost.Startup))]

namespace WebHost
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Trace(outputTemplate: "{Timestamp} [{Level}] ({Name}){NewLine} {Message}{NewLine}{Exception}")
                .CreateLogger();

            var factory = new IdentityServerServiceFactory()
                        .UseInMemoryUsers(Users.Get())
                        .UseInMemoryClients(Clients.Get())
                        .UseInMemoryScopes(Scopes.Get());

            var options = new IdentityServerOptions
            {
                SigningCertificate = Certificate.Load(),
                Factory = factory,
            };

			appBuilder.Use(async (ctx, next) =>
				{
					// Buffer the output response.
					var stream = ctx.Response.Body;
					var buffer = new MemoryStream();
					ctx.Response.Body = buffer;

					await next();

					ctx.Response.Body = stream;
					ctx.Response.Headers["Content-Length"] = buffer.Length.ToString();
					buffer.Seek(0, SeekOrigin.Begin);
					await buffer.CopyToAsync(stream);
				});

            appBuilder.UseIdentityServer(options);
        }
    }
}