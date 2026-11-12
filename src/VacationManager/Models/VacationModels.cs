using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VacationManager.Models;

public enum RoleName
{
    Unassigned = 0,
    Developer = 1,
    TeamLead = 2,
    CEO = 3,
}

public class Role
{
    // имената в базата данни; ползваме тези константи навсякъде при сравнение,
    // защото RoleName.TeamLead.ToString() е "TeamLead" без интервал
    public const string Ceo = "CEO";
    public const string TeamLead = "Team Lead";
    public const string Developer = "Developer";
    public const string Unassigned = "Unassigned";

    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public List<User> Users { get; set; } = new();
}

public class Team
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    public int? LeaderId { get; set; }

    [ForeignKey(nameof(LeaderId))]
    public User? Leader { get; set; }

    public List<User> Members { get; set; } = new();
}

public class Project
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public List<Team> Teams { get; set; } = new();
}

public class User
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public int? TeamId { get; set; }

    [ForeignKey(nameof(TeamId))]
    public Team? Team { get; set; }
}
