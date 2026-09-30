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
builder.Services.AddHttpClient<TMDbClient>();
builder.Services.AddHttpClient<OMDbClient>();
builder.Services.AddRazorPages();

// Build the app and apply any pending migrations.
WebApplication app = builder.Build();
using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MMDbContext>().Database.Migrate();
}

// Configure the request pipeline and run.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.Run();