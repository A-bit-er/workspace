
namespace ADScimApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Use Startup class for configuration
        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();

        // Configure the HTTP request pipeline using Startup
        startup.Configure(app, app.Environment);

        app.Run();
    }
}
