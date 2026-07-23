namespace Shared.Constants;

public static class RoleNames
{
    public const long AdminId = 1;
    public const long LibrarianId = 2;
    public const long LibraryMemberId = 3;

    public const string Admin = "Admin";
    public const string Librarian = "Librarian";
    public const string LibraryMember = "Library Member";

    public static bool IsProtected(long id) =>
        id is AdminId or LibrarianId or LibraryMemberId;
}
