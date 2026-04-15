namespace WebApiWithEndpointMapper.Models;

public abstract class EntityBase
{
    public int Id { get; set; }
}

public class User : EntityBase
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public int? UserLevel { get; set; }
}
