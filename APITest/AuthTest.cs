using APITest.Helpers;
using BearMarkupLanguage;
using BearWordsAPI.Shared.Data;
using BearWordsAPI.RequestHandler;
using BearWordsAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace APITest;

public class SyncTest
{
    private readonly ConfigService _configService;
    private readonly BearWordsContext _context;

    public SyncTest()
    {
        var ml = new BearML();
        ml.AddKeyValue("issuer_key", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");
        _configService = new ConfigService(ml);

        _context = new MockDb().CreateDbContext();
        _context.Users.Add(new BearWordsAPI.Shared.Data.Models.User
        {
            UserName = "admin",
            CreatedAt = Timestamp.GenerateDatetimeLong(1)
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task LoginReturnJWTIfSuccessed()
    {
        var jwt = await RootHandler.PostLogin(new UserLogin("admin"), _context, _configService);

        if (jwt.Result is not Ok<Jwt> j)
            Assert.Fail("Expected Ok<JWT>");
    }

    [Fact]
    public async Task LoginReturnUnauthIfFailed()
    {
        var jwt = await RootHandler.PostLogin(new UserLogin("user123"), _context, _configService);

        if (jwt.Result is not UnauthorizedHttpResult)
            Assert.Fail("Expected UnauthorizedHttpResult");
    }
}
