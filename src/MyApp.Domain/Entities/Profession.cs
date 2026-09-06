namespace MyApp.Domain.Entities;

public class Profession
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public ICollection<User> Users { get; set; } = [];
}
