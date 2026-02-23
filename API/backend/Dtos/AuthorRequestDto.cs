using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public sealed class AuthorRequestDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateOnly BirthDate { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }
}
