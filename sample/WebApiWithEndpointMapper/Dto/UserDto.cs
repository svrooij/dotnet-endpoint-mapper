using WebApiWithEndpointMapper.Endpoints;
using WebApiWithEndpointMapper.Models;

namespace WebApiWithEndpointMapper.Dto;

[Svrooij.EndpointMapper.GenerateSelect(typeof(User))]
[Svrooij.EndpointMapper.GenerateSchema(typeof(UserDtoValidator))]
public class UserDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int? UserLevel { get; set; }

    // Computed from a LINQ expression: User has no direct "DisplayName" property
    [Svrooij.EndpointMapper.SelectExpression("e.Name + \" (\" + e.Email + \")\"")]
    public string? DisplayName { get; set; }
}
