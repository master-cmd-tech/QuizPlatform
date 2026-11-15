using Microsoft.Extensions.Options;
using QuizPlatform.Web.Components;
using QuizPlatform.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHealthChecks();

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));

// Singletons: one shared, thread-safe data snapshot + one live event bus for all circuits.
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IDataStore>(sp =>
{
    var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    var env = sp.GetRequiredService<IHostEnvironment>();
    var path = Path.IsPathRooted(options.Path) ? options.Path : Path.Combine(env.ContentRootPath, options.Path);
    return new JsonDataStore(path, sp.GetRequiredService<ILogger<JsonDataStore>>());
});
builder.Services.AddSingleton<LiveFeed>();
builder.Services.AddSingleton<AccessService>();
builder.Services.AddSingleton<QuizService>();

// Scoped = one per Blazor circuit (browser tab): the client-side state container.
builder.Services.AddScoped<AppState>();
    
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapHealthChecks("/health");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
