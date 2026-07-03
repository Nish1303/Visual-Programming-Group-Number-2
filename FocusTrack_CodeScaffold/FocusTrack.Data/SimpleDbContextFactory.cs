using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Data
{
    /// <summary>
    /// A deliberately minimal <see cref="IDbContextFactory{TContext}"/> implementation.
    ///
    /// We stopped using EF Core's built-in `services.AddDbContextFactory&lt;TContext&gt;(...)`
    /// because its internal DbContextFactory implementation captures an app-level
    /// IServiceProvider reference the first time it's resolved from DI, and — depending on
    /// exactly how/when that first resolution happens — that captured reference can end up
    /// pointing at a short-lived provider that gets disposed, causing
    /// "Cannot access a disposed object: 'IServiceProvider'" on every later DbContext creation.
    ///
    /// This class sidesteps the problem entirely: DbContextOptions are built once, up front,
    /// from a plain connection string — no IServiceProvider is ever captured or passed to
    /// EF Core, so there is nothing that can go stale or get disposed out from under it.
    /// </summary>
    public class SimpleDbContextFactory : IDbContextFactory<FocusTrackDbContext>
    {
        private readonly DbContextOptions<FocusTrackDbContext> _options;

        public SimpleDbContextFactory(string sqliteConnectionString)
        {
            _options = new DbContextOptionsBuilder<FocusTrackDbContext>()
                .UseSqlite(sqliteConnectionString)
                .Options;
        }

        public FocusTrackDbContext CreateDbContext() => new(_options);

        // IDbContextFactory<TContext>.CreateDbContextAsync is provided by EF Core as an
        // extension method that wraps CreateDbContext() in Task.FromResult, so implementing
        // the single synchronous method above is sufficient to satisfy every caller.
    }
}
