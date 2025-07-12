using BearWordsAPI.Shared.Data;
using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace BearWordsAPI.RequestHandler;

public record UserLogin(string UserName);
public record Jwt(string Token);
public record RegisterRequest(string ClientId);

public static class RootHandler
{
    public static async Task<Results<Ok<Jwt>, UnauthorizedHttpResult>> PostLogin
        ([FromBody] UserLogin user, BearWordsContext db, ConfigService config)
    {
        var dbUser = await db.Users.AsNoTracking().WhereUser(user.UserName).FirstOrDefaultAsync();
        if (dbUser is not null)
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.IssuerKey));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Role, "User")
                ]),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "bear_auth",
                Audience = "bear_words",
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return TypedResults.Ok(new Jwt(jwt));
        }

        return TypedResults.Unauthorized();
    }

    public static async Task<Results<Created, Conflict>> PostRegister
        ([FromBody] RegisterRequest registerRequest, ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var sync = await db.Users
            .WhereUser(userName)
            .SelectMany(u => u.Syncs)
            .FirstOrDefaultAsync(s => s.ClientId == registerRequest.ClientId);
        if (sync is null)
        {
            await db.Syncs.AddAsync(new Sync
            {
                UserName = userName,
                ClientId = registerRequest.ClientId,
                LastPull = DateTime.MinValue.ToLong(),
                LastPush = DateTime.MinValue.ToLong()
            });
            await db.SaveChangesAsync();

            return TypedResults.Created();
        }
        else
        {
            return TypedResults.Conflict();
        }
    }

    public static async Task<Results<Ok, NotFound>> PostReregister
        ([FromBody] RegisterRequest registerRequest, ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var sync = await db.Users
            .WhereUser(userName)
            .SelectMany(u => u.Syncs)
            .FirstOrDefaultAsync(s => s.ClientId == registerRequest.ClientId);
        if (sync is not null)
        {
            sync.LastPull = DateTime.MinValue.ToLong();
            sync.LastPush = DateTime.MinValue.ToLong();
            await db.SaveChangesAsync();

            return TypedResults.Ok();
        }
        else
        {
            return TypedResults.NotFound();
        }
    }
}
