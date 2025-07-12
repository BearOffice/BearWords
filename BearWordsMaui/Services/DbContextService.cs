using Microsoft.EntityFrameworkCore;

namespace BearWordsMaui.Services;

public partial class DbContextService : IDbContextService, IDisposable
{
    public event EventHandler? BeforeSaveChanges;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConfigService _config;
    private BearWordsContext? _context;
    private readonly Lock _lock = new Lock();

    public DbContextService(IServiceProvider serviceProvider, ConfigService configService)
    {
        _serviceProvider = serviceProvider;
        _config = configService;
    }

    public BearWordsContext GetDbContext()
    {
        lock (_lock)
        {
            if (_context is null)
            {
                CreateNewDbContext();
            }
            return _context!;
        }
    }

    public void CreateNewDbContext()
    {
        lock (_lock)
        {
            if (_context is not null)
            {
                _context.BeforeSaveChanges -= Context_BeforeSaveChanges;
                _context.Dispose();
            }

            var optionsBuilder = new DbContextOptionsBuilder<BearWordsContext>();
            optionsBuilder.UseSqlite($"Data Source={_config.DatabasePath}");

            // Force EF Core rebuild the model (refresh global filter)
            optionsBuilder.EnableServiceProviderCaching(false);

            _context = new BearWordsContext(optionsBuilder.Options, _serviceProvider);
            _context.BeforeSaveChanges += Context_BeforeSaveChanges;
        }
    }

    private void Context_BeforeSaveChanges(object? sender, EventArgs e)
    {
        BeforeSaveChanges?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (_context is not null)
        {
            _context.BeforeSaveChanges -= Context_BeforeSaveChanges;
            _context.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}