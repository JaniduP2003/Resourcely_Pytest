using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Backend_Resourcely.Data;
using Backend_Resourcely.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Backend_Resourcely.Helpers
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeDatabase(IConfiguration configuration, bool throwOnFailure = false)
        {
            var connectionString = configuration.GetConnectionString("Default");
            
            Console.WriteLine("Starting database initialization...");
            
            try
            {
                // 1) Ensure DB exists (connect to master and create if missing)
                await EnsureDatabaseExistsAsync(connectionString);

                // 2) Run SQL scripts to create schema/seed data (idempotent scripts with IF NOT EXISTS)
                await RunSqlScriptsAsync(connectionString);

                // 3) Create admin user using Entity Framework (idempotent)
                await CreateAdminUser(configuration);
                Console.WriteLine("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                // Handle exceptions with more detailed logging
                Console.WriteLine($"Database initialization error: {ex.Message}");
                Console.WriteLine("The application will continue without database initialization.");
                
                if (throwOnFailure)
                {
                    throw; // Re-throw only if explicitly requested
                }
                
                // Log but don't crash the application
                Console.WriteLine("Skipping database initialization due to connection issues.");
            }
        }

        private static async Task EnsureDatabaseExistsAsync(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var targetDatabase = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(targetDatabase))
            {
                throw new InvalidOperationException("Connection string must include a Database/Initial Catalog.");
            }

            // Connect to master to perform create database
            var masterCs = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            }.ConnectionString;

            const int maxRetries = 10;
            var delayMs = 1500;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var conn = new SqlConnection(masterCs);
                    await conn.OpenAsync();

                    var existsCmd = conn.CreateCommand();
                    existsCmd.CommandText = @"IF DB_ID(@db) IS NULL SELECT 0 ELSE SELECT 1";
                    existsCmd.Parameters.AddWithValue("@db", targetDatabase);
                    var exists = (int)await existsCmd.ExecuteScalarAsync() == 1;

                    if (!exists)
                    {
                        Console.WriteLine($"Database '{targetDatabase}' not found. Creating...");
                        using var createCmd = conn.CreateCommand();
                        createCmd.CommandText = $"CREATE DATABASE [{targetDatabase}]";
                        await createCmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"Database '{targetDatabase}' created.");
                    }

                    // Try to connect to the target DB to ensure it is accessible
                    using var targetConn = new SqlConnection(connectionString);
                    await targetConn.OpenAsync();
                    Console.WriteLine($"Verified connection to database '{targetDatabase}'.");
                    return;
                }
                catch (SqlException sqlEx)
                {
                    // SQL container might still be starting up; retry
                    Console.WriteLine($"[EnsureDatabaseExists] Attempt {attempt}/{maxRetries} failed: {sqlEx.Message}");
                    if (attempt == maxRetries) throw;
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, 8000);
                }
            }
        }

        private static async Task RunSqlScriptsAsync(string connectionString)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var scriptsDir = Path.Combine(baseDir, "SqlScripts");
                if (!Directory.Exists(scriptsDir))
                {
                    Console.WriteLine($"No SqlScripts directory found at '{scriptsDir}'. Skipping script execution.");
                    return;
                }

                var scriptFiles = new[]
                {
                    // Idempotent base schema and seed
                    Path.Combine(scriptsDir, "CreateTables.sql"),
                    // Add columns/indexes; idempotent enough for dev
                    Path.Combine(scriptsDir, "AddPermissionsAndApprovals.sql")
                    // NOTE: Skipping UpdateToHierarchicalSchema.sql because it contains plain CREATE/DROP that may conflict.
                };

                foreach (var file in scriptFiles)
                {
                    if (!File.Exists(file))
                    {
                        Console.WriteLine($"Script not found: {file}. Skipping.");
                        continue;
                    }

                    Console.WriteLine($"Executing SQL script: {Path.GetFileName(file)}");
                    var sql = await File.ReadAllTextAsync(file);
                    await ExecuteSqlBatchesAsync(connectionString, sql);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing SQL scripts: {ex.Message}");
                throw;
            }
        }

        private static async Task ExecuteSqlBatchesAsync(string connectionString, string sql)
        {
            // Split on lines containing only GO (case-insensitive)
            var batches = Regex.Split(sql, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                               .Select(b => b.Trim())
                               .Where(b => !string.IsNullOrWhiteSpace(b));

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            foreach (var batch in batches)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 120; // long-running schema changes
                cmd.CommandText = batch;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task CreateAdminUser(IConfiguration configuration)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 2000;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"Attempting to create admin user (attempt {attempt}/{maxRetries})...");
                    
                    var connectionString = configuration.GetConnectionString("Default");
                    var options = new DbContextOptionsBuilder<AppDbContext>()
                        .UseSqlServer(connectionString, sqlOptions =>
                        {
                            sqlOptions.CommandTimeout(30);
                            sqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 3,
                                maxRetryDelay: TimeSpan.FromSeconds(5),
                                errorNumbersToAdd: null);
                        })
                        .Options;

                    using (var context = new AppDbContext(options))
                    {
                        // Test connection first
                        await context.Database.CanConnectAsync();
                        
                        // Check if admin user already exists
                        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@example.com");
                        if (existingAdmin != null)
                        {
                            Console.WriteLine("Admin user already exists.");
                            return;
                        }

                        // Create admin user with proper password hashing
                        var (hash, salt) = PasswordHelper.HashPassword("admin123");
                        
                        var adminUser = new User
                        {
                            Email = "admin@example.com",
                            Username = "admin",
                            PasswordHash = hash,
                            PasswordSalt = salt,
                            Role = "Admin",
                            CreatedAt = DateTime.UtcNow
                        };

                        context.Users.Add(adminUser);
                        await context.SaveChangesAsync();
                        
                        Console.WriteLine("Admin user created successfully: admin@example.com / admin123");
                        return; // Success, exit retry loop
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating admin user on attempt {attempt}: {ex.Message}");
                    
                    if (attempt < maxRetries)
                    {
                        Console.WriteLine($"Retrying in {retryDelayMs}ms...");
                        await Task.Delay(retryDelayMs);
                    }
                    else
                    {
                        Console.WriteLine("Failed to create admin user after all retry attempts. The app will continue without the admin user.");
                        throw; // Re-throw on final attempt
                    }
                }
            }
        }
    }
}