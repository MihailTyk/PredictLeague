using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;

var builder = WebApplication.CreateBuilder(args);

// 🧱 Настройка на базата данни
builder.Services.AddDbContext<PredictLeagueContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PredictLeagueContext")
        ?? throw new InvalidOperationException("Connection string 'PredictLeagueContext' not found.")));

// ✅ Добавяме Identity (логин / регистрация + роли)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4; // по-лесна парола за тест
})
.AddRoles<IdentityRole>() // 👈 добавяме поддръжка на роли
.AddEntityFrameworkStores<PredictLeagueContext>();

// ✅ Razor Pages (за Login / Register)
builder.Services.AddRazorPages();

// ✅ Контролери и изгледи
builder.Services.AddControllersWithViews();

// ✅ HttpClient за външни API заявки
builder.Services.AddHttpClient();

var app = builder.Build();

// 📦 Инициализация на база данни и примерни данни
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<PredictLeagueContext>();
    context.Database.EnsureCreated();

    // 🏟️ Примерни мачове
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

    // 👑 Създаваме ролята Admin при първо стартиране
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // 👤 Създаваме потребител Admin ако го няма
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    string adminEmail = "admin@predictleague.com";
    string adminPassword = "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine("✅ Admin user created: " + adminEmail);
        }
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
