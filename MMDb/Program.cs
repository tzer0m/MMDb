using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Options;
using MMDb.Services;

// Create web application builder and register services.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MMDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("MMDb")));
builder.Services.Configure<TMDbOptions>(builder.Configuration.GetSection("TMDb"));
builder.Services.Configure<OMDbOptions>(builder.Configuration.GetSection("OMDb"));
builder.Services.Configure<JellyfinOptions>(builder.Configuration.GetSection("Jellyfin"));
builder.Services.Configure<ExternalLinkOptions>(builder.Configuration.GetSection("ExternalLinks"));
builder.Services.Configure<RatingsRefreshOptions>(builder.Configuration.GetSection("RatingsRefresh"));
builder.Services.Configure<PeopleOptions>(builder.Configuration.GetSection("People"));
builder.Services.AddHttpClient<TMDbClient>();
builder.Services.AddHttpClient<OMDbClient>();
builder.Services.AddHttpClient<JellyfinClient>();
builder.Services.AddScoped<FilmDataService>();
builder.Services.AddHostedService<RatingsRefreshService>();

// Sign in with Pocket ID; the cookie keeps me signed in for 30 days.
OidcOptions oidc = builder.Configuration.GetSection("Oidc").Get<OidcOptions>() ?? new OidcOptions();
AuthenticationBuilder authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
});
authentication.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});
authentication.AddOpenIdConnect(options =>
{
    options.Authority = oidc.Authority;
    options.ClientId = oidc.ClientId;
    options.ClientSecret = oidc.ClientSecret;
    options.ResponseType = "code";
    options.UsePkce = true;
    options.MapInboundClaims = false;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Add("email");
    options.TokenValidationParameters.NameClaimType = "name";
    options.Events.OnTicketReceived = context =>
    {
        context.Properties!.IsPersistent = true;
        return Task.CompletedTask;
    };
});

// Trust the Cloudflare Tunnel's forwarded headers so sign-in redirects use https.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Everything is public except adding and editing films.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Films/Add");
    options.Conventions.AuthorizePage("/Films/Edit");
});

// Build the app and apply any pending migrations.
WebApplication app = builder.Build();
using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MMDbContext>().Database.Migrate();
}

// Configure the request pipeline and run.
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.Run();