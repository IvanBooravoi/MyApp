using MyApp.Domain.Entities;

namespace MyApp.Application.Abstractions;

public interface ITokenProvider
{
    string Create(User user);
}
