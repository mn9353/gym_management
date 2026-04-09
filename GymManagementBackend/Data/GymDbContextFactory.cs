using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GymManagementBackend.Data
{
    public class GymDbContextFactory : IDesignTimeDbContextFactory<GymDbContext>
    {
        public GymDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var rawConnectionString = configuration.GetConnectionString("DefaultConnection");
            var resolvedConnectionString = ResolveConnectionString(rawConnectionString);

            var optionsBuilder = new DbContextOptionsBuilder<GymDbContext>();
            optionsBuilder.UseNpgsql(resolvedConnectionString);
            return new GymDbContext(optionsBuilder.Options);
        }

        private static string ResolveConnectionString(string? rawConnectionString)
        {
            if (!string.IsNullOrWhiteSpace(rawConnectionString))
            {
                if (rawConnectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
                {
                    return rawConnectionString;
                }

                if (Uri.TryCreate(rawConnectionString, UriKind.Absolute, out var uri) &&
                    uri.Scheme.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
                {
                    var userInfo = uri.UserInfo.Split(':', 2);
                    var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
                    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
                    var database = uri.AbsolutePath.Trim('/');

                    var builder = new NpgsqlConnectionStringBuilder
                    {
                        Host = uri.Host,
                        Port = uri.Port > 0 ? uri.Port : 5432,
                        Database = string.IsNullOrWhiteSpace(database) ? "postgres" : database,
                        Username = username,
                        Password = password,
                        SslMode = SslMode.Require
                    };

                    return builder.ConnectionString;
                }

                return rawConnectionString;
            }

            return "Host=localhost;Port=5432;Database=gym_management;Username=postgres;Password=postgres";
        }
    }
}
