using System.Text.RegularExpressions;
using Eng.Agilium.Be.Features;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

/// <summary>Walks every endpoint of the API (reflection), so a new endpoint cannot be added without a decision about access.</summary>
public class EndpointMatrixTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private IEnumerable<IEndpoint> AllEndpoints() =>
    typeof(IEndpoint)
      .Assembly.GetTypes()
      .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
      .Select(t => (IEndpoint)ActivatorUtilities.CreateInstance(Factory.Services, t));

  private static string Url(IEndpoint endpoint)
  {
    dynamic e = endpoint;
    string route = e.FullRoute;
    return Regex.Replace(route, @"\{[^}]+\}", "1");
  }

  [Fact]
  public void EveryEndpoint_DeclaresAPolicy_AndProjectPolicyHasAPermissionOrOwnerOnly()
  {
    var endpoints = AllEndpoints().ToList();
    endpoints.Count.ShouldBeGreaterThan(30);
    foreach (dynamic e in endpoints)
    {
      EndpointPolicy policy = e.Policy;
      policy.ShouldNotBeNull((string)e.GetType().FullName);
      if (policy.Access == EndpointAccess.Project)
        (policy.Permission != null || policy.OwnerOnly).ShouldBeTrue((string)e.GetType().FullName);
    }
  }

  [Fact]
  public async Task EveryNonAnonymousEndpoint_RejectsAnonymousCaller_With401()
  {
    foreach (dynamic e in AllEndpoints())
    {
      EndpointPolicy policy = e.Policy;
      if (!policy.RequiresAuthentication)
        continue;
      var method = (Eng.Agilium.Be.Features.HttpMethod)e.Method;
      var path = Url((IEndpoint)e).Replace("/api/v1/", "");
      var response = await Api.Send(new System.Net.Http.HttpMethod(method.ToString().ToUpperInvariant()), path, method == Eng.Agilium.Be.Features.HttpMethod.Get || method == Eng.Agilium.Be.Features.HttpMethod.Delete ? null : new { });
      response.Code.ShouldBe(401, $"{method} {path} ({(string)e.GetType().FullName})");
    }
  }
}
