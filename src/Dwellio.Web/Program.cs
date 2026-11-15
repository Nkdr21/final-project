using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dwellio.Web.Components;
using Dwellio.Web.Components.Account;
using Dwellio.Web.Data;
using Dwellio.Web.Services;
using Dwellio.Web.Services.OpenData;
using Microsoft.Extensions.Options;

// The interface is in English: format numbers and dates the same way on every machine
// (money is formatted explicitly in Kazakh/Russian style by Labels.Money).
System.Globalization.CultureInfo.DefaultThreadCurrentCulture =
    System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var dataPath = builder.Configuration["DWELLIO_DATA_PATH"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataPath);
var connectionString = $"Data Source={Path.Combine(dataPath, "identity.db")}";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // Local classroom version: no email provider configured.
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddDbContextFactory<HousingDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(dataPath, "housing.db")}"));
builder.Services.AddScoped<HousingService>();
builder.Services.AddScoped<PlannerState>();
builder.Services.AddScoped<HomeFinder>();
builder.Services.AddMemoryCache();

// Free, key-less open data: OpenStreetMap (Overpass, Nominatim, Photon) and the Astana GTFS dataset.
builder.Services.Configure<OpenDataOptions>(builder.Configuration.GetSection(OpenDataOptions.Section));
void Polite(IServiceProvider services, HttpClient http) =>
    http.DefaultRequestHeaders.UserAgent.ParseAdd(services.GetRequiredService<IOptions<OpenDataOptions>>().Value.UserAgent);
builder.Services.AddHttpClient(OverpassClient.HttpClientName, (services, http) => { Polite(services, http); http.Timeout = TimeSpan.FromSeconds(120); });
builder.Services.AddSingleton<OverpassClient>();
builder.Services.AddHttpClient<GeocodingService>((services, http) => { Polite(services, http); http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en,ru;q=0.8"); });
builder.Services.AddSingleton<PlaceService>();
builder.Services.AddSingleton<TransitService>();

var app = builder.Build();
await SeedData.Initialize(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

public partial class Program { }
