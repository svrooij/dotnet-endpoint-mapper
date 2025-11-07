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
}
