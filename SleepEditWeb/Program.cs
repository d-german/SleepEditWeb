using SleepEditWeb.Data;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddControllersWithViews();
		builder.Services.AddHttpClient();
		builder.Services.AddHttpClient<IDrugInfoService, OpenFdaDrugInfoService>();

		builder.Services.AddSingleton<IMedicationRepository, LiteDbMedicationRepository>();
		builder.Services.AddScoped<IMedicationNarrativeBuilder, MedicationNarrativeBuilder>();
		builder.Services.AddScoped<IEditorInsertionService, EditorInsertionService>();
		builder.Services.AddScoped<ISleepNoteEditorSessionStore, SleepNoteEditorSessionStore>();
		builder.Services.AddScoped<ISleepNoteEditorOrchestrator, SleepNoteEditorOrchestrator>();
		builder.Services.AddScoped<IProtocolXmlService, ProtocolXmlService>();
		builder.Services.AddScoped<IProtocolStarterService, ProtocolStarterService>();
		builder.Services.AddScoped<IProtocolEditorSessionStore, ProtocolEditorSessionStore>();
		builder.Services.AddScoped<IProtocolEditorService, ProtocolEditorService>();
		builder.Services.AddHttpContextAccessor();

		builder.Services.Configure<SleepNoteEditorFeatureOptions>(
			builder.Configuration.GetSection(SleepNoteEditorFeatureOptions.SectionName));
		builder.Services.Configure<ProtocolEditorFeatureOptions>(
			builder.Configuration.GetSection(ProtocolEditorFeatureOptions.SectionName));
		builder.Services.Configure<ProtocolEditorStartupOptions>(
			builder.Configuration.GetSection(ProtocolEditorStartupOptions.SectionName));

		builder.Services.AddDistributedMemoryCache();
		builder.Services.AddSession(options =>
		{
			options.IdleTimeout = TimeSpan.FromMinutes(30);
			options.Cookie.HttpOnly = true;
			options.Cookie.IsEssential = true;
		});

		var app = builder.Build();

		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Home/Error");
			app.UseHsts();
		}

		app.UseSession();
		app.UseHttpsRedirection();
		app.UseStaticFiles();
		app.UseRouting();
		app.UseAuthorization();

		app.MapControllerRoute(
			name: "default",
			pattern: "{controller=SleepNoteEditor}/{action=Index}/{id?}");

		app.Run();
	}
}
