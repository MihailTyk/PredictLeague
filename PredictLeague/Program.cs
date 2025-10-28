using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;

var builder = WebApplication.CreateBuilder(args);

// 🧱 Настройка на базата данни
builder.Services.AddDbContext<PredictLeagueContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PredictLeagueContext")
        ?? throw new InvalidOperationException("Connection string 'PredictLeagueContext' not found.")));

// ✅ Добавяме Identity за Login / Register
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<PredictLeagueContext>();

// ✅ Добавяме Razor Pages (необходимо за Login / Register UI)
builder.Services.AddRazorPages();

// ✅ Контролери и изгледи
builder.Services.AddControllersWithViews();

// ✅ HttpClient за външни API заявки
builder.Services.AddHttpClient();

var app = builder.Build();

// 📦 Създаваме базата и примерни мачове, ако липсват
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PredictLeagueContext>();
    context.Database.EnsureCreated();

    if (!context.Match.Any())
    {
        context.Match.AddRange(
            new PredictLeague.Models.Match
            {
                HomeTeam = "Barcelona",
                AwayTeam = "Real Madrid",
                StartTime = DateTime.Now.AddDays(1),
                IsFinished = false
            },
            new PredictLeague.Models.Match
            {
                HomeTeam = "Liverpool",
                AwayTeam = "Manchester United",
                StartTime = DateTime.Now.AddDays(2),
                IsFinished = false
            },
            new PredictLeague.Models.Match
            {
                HomeTeam = "Bayern Munich",
                AwayTeam = "Borussia Dortmund",
                StartTime = DateTime.Now.AddDays(3),
                IsFinished = false
            }
        );
        context.SaveChanges();
    }
}

// ⚙️ Middleware настройки
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🧍‍♂️ Identity Middleware
app.UseAuthentication();
app.UseAuthorization();

// 🗺️ Основен MVC маршрут
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 🔑 Razor Pages маршрути (за Login / Register)
app.MapRazorPages();

app.Run();
