namespace Contracts.Author;

using System.ComponentModel.DataAnnotations;

public record class AuthorDto
{
    public long Id { get; set; }

    public string Name { get; set; } = "";
}

public record class CreateAuthorRequest
{
    [Required]
    [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    public string Name { get; set; } = "";
}

public record class UpdateAuthorRequest
{
    [Required]
    [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    public string Name { get; set; } = "";
}

public record class AuthorFilterRequest : Shared.Models.OffsetPagedRequest
{
    public string? Name { get; set; }
}
