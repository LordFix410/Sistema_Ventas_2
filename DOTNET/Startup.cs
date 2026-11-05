using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace ImagenPDF
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.Name = ".LibreriaDigital.Productos";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.IdleTimeout = System.TimeSpan.FromHours(8);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment, IConfiguration configuration)
        {
            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            var productImagesPath = configuration["ProductImages:Path"];
            if (string.IsNullOrWhiteSpace(productImagesPath))
                productImagesPath = Path.Combine(environment.ContentRootPath, "imagenes");
            Directory.CreateDirectory(productImagesPath);
            Directory.CreateDirectory(Path.Combine(productImagesPath, "productos"));
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(productImagesPath),
                RequestPath = "/imagenes"
            });
            app.UseSession();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
