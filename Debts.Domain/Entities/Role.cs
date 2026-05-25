namespace Debts.Domain.Entities;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public Role() { }

    public Role(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    public static class Names
    {
        public const string Admin = "Admin";
        public const string UserAdmin = "UserAdmin";
        public const string DebtAdmin = "DebtAdmin";
    }
}