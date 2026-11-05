using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cookie Authentication
builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options =>
{
    options.Cookie.Name = "MultiverseAuth";
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ILookupService,LookupService>();
builder.Services.AddScoped<ILeadImportExportService,LeadImportExportService>();
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
    

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MultiverseCRM.Data.AppDbContext>();
    db.Database.Migrate();                       // must be migrations, not EnsureCreated
    MultiverseCRM.Data.LookupSeeder.Seed(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();