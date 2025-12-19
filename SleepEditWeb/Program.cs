using SleepEditWeb.Controllers;
using SleepEditWeb.Data;

namespace SleepEditWeb;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		// Add services to the container.
		builder.Services.AddControllersWithViews();
		builder.Services.AddHttpClient(); // Add HttpClient here
		
		// Register LiteDB medication repository as singleton (thread-safe, single connection per app lifetime)
		builder.Services.AddSingleton<IMedicationRepository, LiteDbMedicationRepository>();

		builder.Services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache
		builder.Services.AddSession(options =>
		{
			options.IdleTimeout = TimeSpan.FromMinutes(30); // You can set the timeout here
			options.Cookie.HttpOnly = true;
			options.Cookie.IsEssential = true;
		});

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Home/Error");
			// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			app.UseHsts();
		}

		app.UseSession();
		app.UseHttpsRedirection();
		app.UseStaticFiles();

		app.UseRouting();

		app.UseAuthorization();

		app.MapControllerRoute(
			name: "default",
			pattern: "{controller=MedList}/{action=Index}/{id?}");

		app.Run();
	}
}