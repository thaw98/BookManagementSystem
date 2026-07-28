namespace Contracts.Category;

using System.ComponentModel.DataAnnotations;

public record class CategoryDto
{
    public long Id { get; set; }

    public string Name { get; set; } = "";
}

public record class CreateCategoryRequest
{
    [Required]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = "";
}

public record class UpdateCategoryRequest
{
    [Required]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = "";
}

public record class CategoryFilterRequest : Shared.Models.OffsetPagedRequest
{
    public string? Name { get; set; }
}
