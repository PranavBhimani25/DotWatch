using Prometheus;
namespace DotWatch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Add metrics endpoint
            app.UseHttpMetrics();       // Collect HTTP metrics
            app.MapMetrics();           // Make /metrics endpoint
            app.MapControllers();
            //app.MapGet("/", () => "Welcome to .NET Monitoring App");

            app.MapGet("/error", () =>
            {
                throw new Exception("Simulated server error");
            });

            app.MapGet("/simulate500", () =>
            {
                throw new Exception("Simulated 500 error");
            });
            // ---------------------
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=WelcomePage}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
