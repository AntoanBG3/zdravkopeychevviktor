using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VacationManager.Models;

namespace VacationManager.Models;

public enum VacationType
{
    Paid = 0,
    Unpaid = 1,
    Sick = 2,
}

public class VacationRequest
{
    public int Id { get; set; }

    public VacationType Type { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime FromDate { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime ToDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime CreatedOn { get; set; }

    public bool HalfDay { get; set; }

    public bool Approved { get; set; }

    public int RequesterId { get; set; }

    [ForeignKey(nameof(RequesterId))]
    public User Requester { get; set; } = null!;

    [StringLength(200)]
    public string? SickNoteFileName { get; set; }

    [StringLength(200)]
    public string? SickNoteContentType { get; set; }

    public byte[]? SickNoteData { get; set; }
}
