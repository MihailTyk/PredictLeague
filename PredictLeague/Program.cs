using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PredictLeague.Data;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PredictLeagueContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PredictLeagueContext") ?? throw new InvalidOperationException("Connection string 'PredictLeagueContext' not found.")));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();
// Добавяме начални примерни мачове при стартиране
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PredictLeague.Data.PredictLeagueContext>();

    // Ако няма мачове, добавяме няколко
    if (!context.Match.Any())
    {
        context.Match.AddRange(
            new PredictLeague.Models.Match { HomeTeam = "Barcelona", AwayTeam = "Real Madrid", StartTime = DateTime.Now.AddDays(1), IsFinished = false },
            new PredictLeague.Models.Match { HomeTeam = "Liverpool", AwayTeam = "Manchester United", StartTime = DateTime.Now.AddDays(2), IsFinished = false },
            new PredictLeague.Models.Match { HomeTeam = "Bayern Munich", AwayTeam = "Borussia Dortmund", StartTime = DateTime.Now.AddDays(3), IsFinished = false }
        );
        context.SaveChanges();
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Matches}/{action=Index}/{id?}");


app.Run();
