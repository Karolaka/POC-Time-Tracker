using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Timesheet.Domain;

namespace Timesheet.Web.Services;

/// <summary>
/// POC-only auth provider. Lets you switch the "signed in" user via UI to exercise role-based flows
/// without a real Entra ID integration.
/// </summary>
public class FakeAuthenticationStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _current = new(new ClaimsIdentity());

    public AppUser? CurrentUser { get; private set; }

    public void SignInAs(AppUser user)
    {
        CurrentUser = user;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName),
            new("Organization", user.Organization.ToString())
        };
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.ToString())));

        _current = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "FakePOCAuth"));
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_current)));
    }

    public void SignOut()
    {
        CurrentUser = null;
        _current = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_current)));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_current));
}
