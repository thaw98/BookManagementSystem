namespace Contracts.Category;

public class CategoryDto
{
    public long Id { get; set; }

    public string Name { get; set; } = "";
}

public sealed class CreateCategoryRequest
{
    public string Name { get; set; } = "";
}

public sealed class UpdateCategoryRequest
{
    public string Name { get; set; } = "";
}