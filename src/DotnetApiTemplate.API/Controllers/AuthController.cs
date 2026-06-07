using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DotnetApiTemplate.API.Controllers;

/// <summary>
/// DEMO authentication endpoint.
///
/// This issues a signed JWT for any supplied username WITHOUT verifying credentials.
/// It exists so you can try the <c>[Authorize]</c>-protected endpoints out of the box.
/// Replace it with a real identity flow (validate credentials against your user store,
/// add roles/claims, issue refresh tokens) before using this in production.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
public class AuthController(IConfiguration configuration, IWebHostEnvironment environment) : BaseApiController
{
    /// <summary>
    /// Issues a demo JWT for the given username (no credential check — see class remarks).
    /// The route is inert (returns 404) unless the app runs in the Development environment
    /// AND <c>Auth:EnableDemoTokenEndpoint</c> is explicitly set to <c>true</c>.
    /// </summary>
    /// <param name="request">The username to embed in the token.</param>
    /// <returns>A bearer token and its expiry.</returns>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TokenResponse> CreateToken([FromBody] TokenRequest request)
    {
        // Defense in depth: never mint credential-free tokens outside an explicitly
        // opted-in Development environment. A misconfigured production deploy that sets
        // the flag is also caught at startup (see AddApplicationServices).
        var demoEnabled = configuration.GetValue("Auth:EnableDemoTokenEndpoint", false);
        if (!environment.IsDevelopment() || !demoEnabled)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest("Username is required.");
        }

        var jwt = configuration.GetSection("Jwt");
        var signingKey = jwt.GetValue<string>("SigningKey")!;

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddHours(1);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.GetValue<string>("Issuer"),
            Audience = jwt.GetValue<string>("Audience"),
            Expires = expires,
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, request.Username),
                new Claim(ClaimTypes.Name, request.Username)
            ])
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return Ok(new TokenResponse(token, expires));
    }
}

/// <summary>Demo token request.</summary>
/// <param name="Username">The username to embed in the issued token.</param>
public record TokenRequest(string Username);

/// <summary>Demo token response.</summary>
/// <param name="AccessToken">The signed JWT bearer token.</param>
/// <param name="ExpiresAt">UTC expiry of the token.</param>
public record TokenResponse(string AccessToken, DateTime ExpiresAt);
