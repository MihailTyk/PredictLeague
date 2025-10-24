using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;

var builder = WebApplication.CreateBuilder(args);

// Настройка на базата данни
builder.Services.AddDbContext<PredictLeagueContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PredictLeagueContext")
        ?? throw new InvalidOperationException("Connection string 'PredictLeagueContext' not found.")));

// Добавяме контролери и изгледи
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Добавяме примерни мачове при стартиране
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

// Настройки за грешки и HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 👉 Основен маршрут — стартира на Home Page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
