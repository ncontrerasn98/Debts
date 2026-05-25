namespace Debts.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public ICollection<Debt> Debts { get; set; } = new List<Debt>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    
    public User()
    {
    }
    public User(string name, string passwordHash)
    {
        Id = Guid.NewGuid();
        Name = name;
        PasswordHash = passwordHash;
    }
}