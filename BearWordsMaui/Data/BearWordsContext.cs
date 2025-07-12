using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;
using BearWordsMaui.Services;
using Microsoft.EntityFrameworkCore;
using SharedBearWordsContext = BearWordsAPI.Shared.Data.BearWordsContext;

namespace BearWordsMaui.Data;

public partial class BearWordsContext : SharedBearWordsContext
{
    public event EventHandler? BeforeSaveChanges;
    private readonly IDateTimeService _dateTimeService;
    private readonly CascadeConfiguration _cascadeConfiguration;
    private readonly ConfigService _config;

    public BearWordsContext(IServiceProvider serviceProvider)
    {
        _dateTimeService = serviceProvider.GetRequiredService<IDateTimeService>();
        _cascadeConfiguration = serviceProvider.GetRequiredService<CascadeConfiguration>();
        _config = serviceProvider.GetRequiredService<ConfigService>();
    }

    public BearWordsContext(DbContextOptions options, IServiceProvider serviceProvider)
        : base(options)
    {
        _dateTimeService = serviceProvider.GetRequiredService<IDateTimeService>();
        _cascadeConfiguration = serviceProvider.GetRequiredService<CascadeConfiguration>();
        _config = serviceProvider.GetRequiredService<ConfigService>();
    }

    // Filter user data in context.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var userName = _config.UserName;

        modelBuilder.Entity<Bookmark>()
            .HasQueryFilter(b => b.UserName == userName);

        modelBuilder.Entity<BookmarkTag>()
            .HasQueryFilter(bt => bt.Bookmark.UserName == userName);

        modelBuilder.Entity<ConflictLog>()
            .HasQueryFilter(c => c.UserName == userName);

        modelBuilder.Entity<Phrase>()
            .HasQueryFilter(p => p.UserName == userName);

        modelBuilder.Entity<PhraseTag>()
            .HasQueryFilter(p => p.Phrase.UserName == userName);

        modelBuilder.Entity<Sync>()
            .HasQueryFilter(s => s.UserName == userName);

        modelBuilder.Entity<Tag>()
            .HasQueryFilter(t => t.TagCategory.UserName == userName);

        modelBuilder.Entity<TagCategory>()
            .HasQueryFilter(tc => tc.UserName == userName);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.UserName == userName);

        base.OnModelCreating(modelBuilder);
    }

    public async Task ClearDbAsync()
    {
        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();
    }

    public async Task ClearUserDataAsync()
    {
        await Users.ExecuteDeleteAsync();
    }

    public async Task ClearUserDataAsync(string userName)
    {
        var user = await Users.FindAsync(userName) ?? throw new Exception($"User `{userName}` not exists.");

        Users.Remove(user);
        await SaveChangesAsync();
    }

    public async Task EnsureClientIdAsync(string userName, string clientId)
    {
        var user = await Users
            .Include(u => u.Syncs)
            .FirstOrDefaultAsync(u => u.UserName == userName)
            ?? throw new Exception($"User `{userName}` not exists.");

        var client = user.Syncs.FirstOrDefault(x => x.ClientId == clientId);
        if (client is null)
        {
            await Syncs.AddAsync(new Sync()
            {
                UserName = userName,
                ClientId = clientId,
                LastPull = DateTime.MinValue.ToLong(),
                LastPush = DateTime.MinValue.ToLong(),
            });
            await SaveChangesAsync();
        }
    }

    public async Task EnsureUserAsync(string userName)
    {
        var user = await Users.FindAsync(userName);
        if (user is not null) return;

        await Users.AddAsync(new User
        {
            UserName = userName,
            CreatedAt = _dateTimeService.GetCurrentTicksLong()
        });
        await SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync(bool updateTimestamps = false, bool cascadeSoftDelete = false,
        CancellationToken cancellationToken = default)
    {
        BeforeSaveChanges?.Invoke(this, EventArgs.Empty);

        if (cascadeSoftDelete) this.CascadeSoftDeleteAndRestore(cascadeConfig: _cascadeConfiguration);
        if (updateTimestamps) UpdateTimestamps();

        return await base.SaveChangesAsync(cancellationToken);
    }

    public int SaveChanges(bool updateTimestamps = false, bool cascadeSoftDelete = false)
    {
        BeforeSaveChanges?.Invoke(this, EventArgs.Empty);

        if (cascadeSoftDelete) this.CascadeSoftDeleteAndRestore(cascadeConfig: _cascadeConfiguration);
        if (updateTimestamps) UpdateTimestamps();

        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var timestampEntries = ChangeTracker.Entries<ITimestamps>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var currentTicks = _dateTimeService.GetCurrentTicksLong();

        foreach (var entry in timestampEntries)
        {
            entry.Entity.ModifiedAt = currentTicks;
        }
    }
}
