namespace Debts.API.Contracts.Requests;

public class CreateUserRequest
{
    public string Name { get; set; }
    public string Password { get; set; }
}