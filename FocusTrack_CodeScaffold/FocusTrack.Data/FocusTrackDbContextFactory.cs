using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FocusTrack.Data
{
    /// <summary>
    /// Used only by EF Core tools (`dotnet ef migrations add`, `dotnet ef database update`)
    /// at design time. The running application never uses this class — it gets its
    /// FocusTrackDbContext through IDbContextFactory via the DI container set up in
    /// FocusTrack.UI/Program.cs instead. Without this factory, `dotnet ef` has no way to
    /// construct FocusTrackDbContext, because its only constructor requires
    /// DbContextOptions&lt;FocusTrackDbContext&gt; and there is no ASP.NET Core host here
    /// for the tooling to discover that configuration from automatically.
    /// </summary>
    public class FocusTrackDbContextFactory : IDesignTimeDbContextFactory<FocusTrackDbContext>
    {
        public FocusTrackDbContext CreateDbContext(string[] args)
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FocusTrack", "focustrack.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            var optionsBuilder = new DbContextOptionsBuilder<FocusTrackDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");

            return new FocusTrackDbContext(optionsBuilder.Options);
        }
    }
}
