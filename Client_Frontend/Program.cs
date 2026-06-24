using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Client_Frontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            // Needed by _ManagerLayout to read Session (username) from the Razor view
            builder.Services.AddHttpContextAccessor();

            // Session storage (for access token)
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromHours(8);
            });

            // HttpClient configured to call the API backend.
            // Set ApiBaseUrl in appsettings or launchSettings; fallback is a common local url.
            builder.Services.AddHttpClient("Api", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7243/");
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}
