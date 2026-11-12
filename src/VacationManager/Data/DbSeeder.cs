using Microsoft.EntityFrameworkCore;
using VacationManager.Models;

namespace VacationManager.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();

        // идемпотентно осеменяване – само ако базата е празна
        if (await context.Roles.AnyAsync())
        {
            return;
        }

        Console.WriteLine("Seeding базата данни...");

        context.Roles.AddRange(
            new Role { Id = 1, Name = Role.Ceo },
            new Role { Id = 2, Name = Role.TeamLead },
            new Role { Id = 3, Name = Role.Developer },
            new Role { Id = 4, Name = Role.Unassigned });

        var ceo = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FirstName = "Главен",
            LastName = "Администратор",
            RoleId = 1,
        };
        context.Users.Add(ceo);

        var lead = new User
        {
            Username = "lead",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Lead123!"),
            FirstName = "Иван",
            LastName = "Иванов",
            RoleId = 2,
        };
        context.Users.Add(lead);

        var dev = new User
        {
            Username = "dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dev123!"),
            FirstName = "Петър",
            LastName = "Петров",
            RoleId = 3,
        };
        context.Users.Add(dev);

        await context.SaveChangesAsync();

        var project = new Project { Name = "Вътрешна платформа", Description = "Разработка на вътрешна платформа за управление на отпуски и екипи." };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var team = new Team { Name = "Development", ProjectId = project.Id, LeaderId = lead.Id };
        team.Members.Add(dev);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        lead.TeamId = team.Id;
        await context.SaveChangesAsync();
    }
}
