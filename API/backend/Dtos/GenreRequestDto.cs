using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public sealed class GenreRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
