using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using SleepEditWeb.Application.Protocol.Commands;
using SleepEditWeb.Application.Protocol.Queries;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Data;
using SleepEditWeb.Infrastructure.ProtocolXml;
using SleepEditWeb.Models;
using SleepEditWeb.Services;
using SleepEditWeb.Components;
using SleepEditWeb.Infrastructure.SleepNote;
using SleepEditWeb.Web.ProtocolEditor;
using SleepEditWeb.Web.AdminAccess;
using Microsoft.AspNetCore.StaticFiles;

namespace SleepEditWeb;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddControllersWithViews();
		builder.Services.AddRazorComponents().AddInteractiveServerComponents();
		builder.Services.AddHttpClient();
		builder.Services.AddHttpClient<IDrugInfoService, OpenFdaDrugInfoService>();

		builder.Services.AddSingleton<IMedicationRepository, LiteDbMedicationRepository>();
		builder.Services.AddScoped<IMedicationNarrativeBuilder, MedicationNarrativeBuilder>();
		builder.Services.AddScoped<IEditorInsertionService, EditorInsertionService>();
		builder.Services.AddScoped<ISleepNoteEditorSessionStore, SleepNoteEditorSessionStore>();
		builder.Services.AddScoped<ISleepNoteEditorOrchestrator, SleepNoteEditorOrchestrator>();
		builder.Services.AddSingleton<IProtocolXmlService, ProtocolXmlService>();
		builder.Services.AddScoped<IProtocolStarterService, ProtocolStarterService>();
		builder.Services.AddScoped<IProtocolEditorSessionStore, ProtocolEditorSessionStore>();
		builder.Services.AddScoped<IProtocolEditorRequestValidator, ProtocolEditorRequestValidator>();
		builder.Services.AddScoped<IProtocolEditorResponseMapper, ProtocolEditorResponseMapper>();
		builder.Services.AddSingleton<IProtocolXmlMapper, ProtocolXmlMapper>();
		builder.Services.AddSingleton<IProtocolXmlSerializer, ProtocolXmlSerializer>();
		builder.Services.AddSingleton<IProtocolXmlDeserializer, ProtocolXmlDeserializer>();
		builder.Services.AddSingleton<LiteDB.LiteDatabase>(_ =>
		{
			var basePath = Environment.OSVersion.Platform == PlatformID.Unix
				? "/app/Data"
				: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
			Directory.CreateDirectory(basePath);
			return new LiteDB.LiteDatabase(Path.Combine(basePath, "sleepeditweb.db"));
		});
		builder.Services.AddSingleton<IProtocolRepository, LiteDbProtocolRepository>();
		builder.Services.AddSingleton<ISleepNoteConfigRepository, LiteDbSleepNoteConfigRepository>();
		builder.Services.AddScoped<ISleepNoteService, SleepNoteService>();
		builder.Services.AddScoped<IProtocolCommandHandler<AddSectionCommand>, AddSectionCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<AddChildCommand>, AddChildCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<RemoveNodeCommand>, RemoveNodeCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<UpdateNodeCommand>, UpdateNodeCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<MoveNodeCommand>, MoveNodeCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<AddSubTextCommand>, AddSubTextCommandHandler>();
		builder.Services.AddScoped<IProtocolCommandHandler<RemoveSubTextCommand>, RemoveSubTextCommandHandler>();
		builder.Services.AddScoped<IProtocolQueryHandler<FindNodeByIdQuery, SleepEditWeb.Protocol.Domain.ProtocolTreeNode>, FindNodeByIdQueryHandler>();
		builder.Services.AddScoped<IProtocolEditorService, ProtocolEditorService>();
		builder.Services.AddScoped<IProtocolManagementService, ProtocolManagementService>();
		builder.Services.AddHttpContextAccessor();

		builder.Services.Configure<SleepNoteEditorFeatureOptions>(
			builder.Configuration.GetSection(SleepNoteEditorFeatureOptions.SectionName));
		builder.Services.Configure<ProtocolEditorFeatureOptions>(
			builder.Configuration.GetSection(ProtocolEditorFeatureOptions.SectionName));

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

		// Cross-Origin Isolation + CSP for WASM-based speech recognition (Vosk-Browser).
		// COOP/COEP enable SharedArrayBuffer required by multi-threaded WASM.
		app.Use(async (context, next) =>
		{
			context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
			context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "credentialless");
			context.Response.Headers.Append("Content-Security-Policy",
				"default-src 'self'; " +
				"script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval' 'unsafe-inline'; " +
				"style-src 'self' 'unsafe-inline'; " +
				"connect-src 'self' data: ws: wss:; " +
				"img-src 'self' data:; " +
				"font-src 'self' data:; " +
				"worker-src 'self' blob:; " +
				"object-src 'none'");
			await next(context);
		});

		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Home/Error");
			app.UseHsts();
		}

		app.UseForwardedHeaders();
		app.UseSession();
		app.UseMiddleware<AdminPasswordMiddleware>();
		app.UseHttpsRedirection();
		var mimeProvider = new FileExtensionContentTypeProvider();
		mimeProvider.Mappings[".gz"] = "application/gzip";
		mimeProvider.Mappings[".wasm"] = "application/wasm";
		mimeProvider.Mappings[".onnx"] = "application/octet-stream";

		app.UseStaticFiles(new StaticFileOptions
		{
			ContentTypeProvider = mimeProvider,
			OnPrepareResponse = ctx =>
			{
				ctx.Context.Response.Headers.Append(
					"Cross-Origin-Resource-Policy", "same-origin");

				var path = ctx.Context.Request.Path.Value ?? string.Empty;
				if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ||
					path.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase) ||
					path.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
				{
					ctx.Context.Response.Headers["Cache-Control"] =
						"public, max-age=604800, immutable";
				}
			}
		});
		app.UseRouting();
		app.UseAuthorization();

		app.MapControllerRoute(
			name: "default",
			pattern: "{controller=SleepNoteEditor}/{action=Index}/{id?}");

		app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

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
