using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using SleepEditWeb.Application.Protocol.Commands;
using SleepEditWeb.Application.Protocol.Queries;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Data;
using SleepEditWeb.Infrastructure.ProtocolXml;
using SleepEditWeb.Models;
using SleepEditWeb.Services;
using SleepEditWeb.Web.ProtocolEditor;

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
		builder.Services.AddScoped<IProtocolEditorPathPolicy, ProtocolEditorPathPolicy>();
		builder.Services.AddScoped<IProtocolEditorFileStore, ProtocolEditorFileStore>();
		builder.Services.AddScoped<IProtocolEditorRequestValidator, ProtocolEditorRequestValidator>();
		builder.Services.AddScoped<IProtocolEditorResponseMapper, ProtocolEditorResponseMapper>();
		builder.Services.AddScoped<IProtocolXmlMapper, ProtocolXmlMapper>();
		builder.Services.AddScoped<IProtocolXmlSerializer, ProtocolXmlSerializer>();
		builder.Services.AddScoped<IProtocolXmlDeserializer, ProtocolXmlDeserializer>();
		builder.Services.AddSingleton<IProtocolRepository, LiteDbProtocolRepository>();
		builder.Services.AddScoped<IProtocolCommandHandler<AddSectionCommand>, AddSectionCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<AddChildCommand>, AddChildCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<RemoveNodeCommand>, RemoveNodeCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<UpdateNodeCommand>, UpdateNodeCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<MoveNodeCommand>, MoveNodeCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<AddSubTextCommand>, AddSubTextCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<RemoveSubTextCommand>, RemoveSubTextCommandHandler>();
		builder.Services.AddScoped<IProtocolQueryHandler<FindNodeByIdQuery, SleepEditWeb.Protocol.Domain.ProtocolTreeNode>, FindNodeByIdQueryHandler>();
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

		ConfigureDataProtection(builder);
		ConfigureForwardedHeaders(builder);

		var app = builder.Build();

		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Home/Error");
			app.UseHsts();
		}

		app.UseForwardedHeaders();
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


	private static void ConfigureDataProtection(WebApplicationBuilder builder)
	{
		var configuredKeyRingPath = builder.Configuration["DataProtection:KeyRingPath"];
		var keyRingPath = string.IsNullOrWhiteSpace(configuredKeyRingPath)
			? Path.Combine(AppContext.BaseDirectory, "Data", "keys")
			: configuredKeyRingPath;

		Directory.CreateDirectory(keyRingPath);

		var dataProtectionBuilder = builder.Services.AddDataProtection();
		dataProtectionBuilder.SetApplicationName("SleepEditWeb");
		dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
	}

	private static void ConfigureForwardedHeaders(WebApplicationBuilder builder)
	{
		builder.Services.Configure<ForwardedHeadersOptions>(options =>
		{
			options.ForwardedHeaders =
				ForwardedHeaders.XForwardedFor |
				ForwardedHeaders.XForwardedProto;

			// Allow reverse-proxy forwarded headers in hosted environments with dynamic proxy addresses.
			options.KnownNetworks.Clear();
			options.KnownProxies.Clear();
		});
	}
}
