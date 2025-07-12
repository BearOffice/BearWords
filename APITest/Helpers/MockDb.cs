using Microsoft.EntityFrameworkCore;
using BearWordsAPI.Shared.Data;

namespace APITest.Helpers;

public class MockDb : IDbContextFactory<BearWordsContext>
{
    public BearWordsContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BearWordsContext>()
            .UseInMemoryDatabase($"InMemoryTestDb-{DateTime.Now.ToFileTimeUtc()}")
            .Options;

        return new BearWordsContext(options);
    }
}

