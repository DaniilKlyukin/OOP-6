using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        string connection = builder.Configuration.GetConnectionString("DefaultConnection");

        // добавляем контекст ApplicationContext в качестве сервиса в приложение
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));

        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        app.MapDefaultControllerRoute();

        app.Run();
    }
}
