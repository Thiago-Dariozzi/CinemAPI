using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace CinemAPI.Tests.Architecture;

public sealed class ContractTests
{
    private static readonly Assembly ApplicationAssembly = typeof(global::Application.Services.UserService).Assembly;
    private static readonly Assembly DomainAssembly = typeof(global::Domain.Entities.User).Assembly;

    [Fact]
    public void Responses_NeverExposePassword()
    {
        var responseTypes = ApplicationAssembly.GetTypes().Where(t => t.Name.EndsWith("Response", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(responseTypes);
        Assert.All(responseTypes, t => Assert.Null(t.GetProperty("Password")));
    }

    [Fact]
    public void Requests_NeverAcceptIdOrIsActive()
    {
        var requestTypes = ApplicationAssembly.GetTypes().Where(t => t.Name.EndsWith("Request", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(requestTypes);
        Assert.All(requestTypes, t =>
        {
            Assert.Null(t.GetProperty("Id"));
            Assert.Null(t.GetProperty("IsActive"));
        });
    }

    // MVC tira InvalidOperationException (500 en cada request) si un record posicional tiene
    // atributos de validación en la propiedad ([property: Required]) en vez del parámetro.
    [Fact]
    public void Requests_PutValidationOnConstructorParameters_NotProperties()
    {
        var offending = ApplicationAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Request", StringComparison.Ordinal))
            .SelectMany(t => t.GetProperties().Select(p => (Type: t, Property: p)))
            .Where(x => x.Property.GetCustomAttributes<System.ComponentModel.DataAnnotations.ValidationAttribute>().Any())
            .Select(x => $"{x.Type.Name}.{x.Property.Name}")
            .ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void Controllers_NeitherReceiveNorReturnDomainEntities()
    {
        var actions = typeof(global::WebApi.Controllers.MovieController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .ToList();

        Assert.NotEmpty(actions);
        Assert.All(actions, action =>
        {
            Assert.DoesNotContain(action.GetParameters(), p => p.ParameterType.Assembly == DomainAssembly);
            Assert.False(MentionsDomainType(action.ReturnType), $"{action.DeclaringType!.Name}.{action.Name} devuelve una entidad.");
        });
    }

    private static bool MentionsDomainType(Type type) =>
        type.Assembly == DomainAssembly || (type.IsGenericType && type.GetGenericArguments().Any(MentionsDomainType));
}
