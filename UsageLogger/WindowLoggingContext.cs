using System.Data.Entity;
using System.Data.SQLite;

namespace UsageLogger
{
    class WindowLoggingContext : DbContext
    {
        public WindowLoggingContext() : base("name=WindowLoggingContext")
        {
            Database.ExecuteSqlCommand(@"
                CREATE TABLE IF NOT EXISTS WindowLogs (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    Start TEXT NOT NULL,
                    End TEXT,
                    ProgramName TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Screenshots (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    Image BLOB,
                    WindowLog_Id INTEGER NOT NULL,
                    FOREIGN KEY(WindowLog_Id) REFERENCES WindowLogs(Id)
                );
            ");
        }

        public DbSet<WindowLog> WindowLogs { get; set; }
        public DbSet<Screenshot> Screenshots { get; set; }
    }
}
