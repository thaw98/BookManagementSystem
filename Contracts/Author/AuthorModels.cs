namespace Contracts.Author;

public class AuthorDto
{
    public long Id { get; set; }

    public string Name { get; set; } = "";
}

public sealed class CreateAuthorRequest
{
    public string Name { get; set; } = "";
}

public sealed class UpdateAuthorRequest
{
    public string Name { get; set; } = "";
}

public sealed class AuthorFilterRequest : Shared.Models.OffsetPagedRequest
{
    public string? Name { get; set; }
}
