using LiteDB;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using ProjectBullet.Core.Entities;
using ProjectBullet.Core.Models.Jobs;
using RuriLib.Models.Proxies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectBullet.Core.Services;

public class MigrationOptions
{
    public bool MigrateConfigs { get; set; } = true;
    public bool MigrateProxies { get; set; } = true;
    public bool MigrateWordlists { get; set; } = true;
    public bool MigrateJobs { get; set; } = true;
    public bool MigrateHits { get; set; } = true;
}

public class MigrationResult
{
    public int ConfigsMigrated { get; set; }
    public int ConfigsSkipped { get; set; }
    public int ProxyGroupsMigrated { get; set; }
    public int ProxyGroupsSkipped { get; set; }
    public int ProxiesMigrated { get; set; }
    public int WordlistsMigrated { get; set; }
    public int WordlistsSkipped { get; set; }
    public int JobsMigrated { get; set; }
    public int HitsMigrated { get; set; }
    public int RecordsMigrated { get; set; }
    public List<string> Errors { get; set; } = new();
}

public enum OB2DbFormat { Unknown, LiteDB, SQLite }

public class OB2MigrationService
{
    private readonly ApplicationDbContext context;
    private readonly MarketplaceApiService _api;

    public OB2MigrationService(ApplicationDbContext context, MarketplaceApiService api)
    {
        this.context = context;
        _api = api;
    }

    private void UploadFileInBackground(string filePath, string type, string name = null)
    {
        if (_api == null || !File.Exists(filePath)) return;
        _ = Task.Run(async () =>
        {
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                var displayName = name ?? Path.GetFileNameWithoutExtension(filePath);
                var ext = Path.GetExtension(filePath);
                var fileName = $"{type}_{displayName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}";
                await _api.UploadUserFileAsync(fileName, type, bytes, fileName);
            }
            catch { }
        });
    }

    private void UploadProxiesInBackground(List<ProxyEntity> proxies, string groupName)
    {
        if (_api == null || proxies.Count == 0) return;
        _ = Task.Run(async () =>
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var p in proxies)
                {
                    var line = string.IsNullOrEmpty(p.Username)
                        ? $"{p.Host}:{p.Port}"
                        : $"{p.Username}:{p.Password}@{p.Host}:{p.Port}";
                    sb.AppendLine(line);
                }
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var fileName = $"proxies_{groupName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
                await _api.UploadUserFileAsync(fileName, "proxy", bytes, fileName);
            }
            catch { }
        });
    }

    /// <summary>
    /// Detects whether a database file is SQLite or LiteDB by reading the header bytes.
    /// </summary>
    public static OB2DbFormat DetectDbFormat(string dbPath)
    {
        try
        {
            using var fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[16];
            if (fs.Read(header, 0, 16) < 16) return OB2DbFormat.Unknown;

            var headerStr = System.Text.Encoding.ASCII.GetString(header);
            if (headerStr.StartsWith("SQLite format 3"))
                return OB2DbFormat.SQLite;

            // LiteDB v5 files start with a specific header page
            return OB2DbFormat.LiteDB;
        }
        catch
        {
            return OB2DbFormat.Unknown;
        }
    }

    /// <summary>
    /// Validates that the given folder is a valid OB2 installation directory.
    /// Returns the path to the database file, or null if invalid.
    /// </summary>
    public static string ValidateOB2Folder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return null;

        var dbNames = new[] { "OpenBullet2.db", "openbullet2.db", "openBullet2.db",
                              "OpenBullet.db", "openbullet.db" };

        var searchDirs = new List<string> { folderPath };

        var userDataPath = Path.Combine(folderPath, "UserData");
        if (Directory.Exists(userDataPath))
            searchDirs.Add(userDataPath);

        var dbSubPath = Path.Combine(userDataPath, "Database");
        if (Directory.Exists(dbSubPath))
            searchDirs.Add(dbSubPath);

        foreach (var dir in searchDirs)
        {
            foreach (var dbName in dbNames)
            {
                var dbPath = Path.Combine(dir, dbName);
                if (File.Exists(dbPath))
                    return dbPath;
            }
        }

        // Last resort: find any .db file that looks like an OB2 database
        foreach (var dir in searchDirs)
        {
            try
            {
                foreach (var file in Directory.GetFiles(dir, "*.db"))
                {
                    var name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if (name.Contains("openbullet"))
                        return file;
                }
            }
            catch { }
        }

        return null;
    }

    public async Task<MigrationResult> MigrateAsync(
        string ob2FolderPath,
        MigrationOptions options,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var result = new MigrationResult();
        var dbPath = ValidateOB2Folder(ob2FolderPath);

        if (dbPath == null)
            throw new InvalidOperationException("Invalid OpenBullet 2 folder. Database file not found.");

        var format = DetectDbFormat(dbPath);
        progress?.Report(($"Detected database format: {format}", 0, 0));

        // 1. Configs (no DB needed, pure file copy)
        if (options.MigrateConfigs)
        {
            ct.ThrowIfCancellationRequested();
            MigrateConfigs(ob2FolderPath, result, progress, ct);
        }

        // 2-5. DB-dependent migrations
        if (format == OB2DbFormat.SQLite)
        {
            await MigrateFromSqliteAsync(dbPath, ob2FolderPath, options, result, progress, ct);
        }
        else if (format == OB2DbFormat.LiteDB)
        {
            await MigrateFromLiteDbAsync(dbPath, ob2FolderPath, options, result, progress, ct);
        }
        else
        {
            result.Errors.Add($"Unknown database format for: {dbPath}");
        }

        progress?.Report(("Migration completed!", 0, 0));
        return result;
    }

    #region Configs
    private void MigrateConfigs(
        string ob2FolderPath, MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var sourceDir = Path.Combine(ob2FolderPath, "UserData", "Configs");
        if (!Directory.Exists(sourceDir))
        {
            result.Errors.Add("Configs folder not found in OB2 directory.");
            return;
        }

        var destDir = Path.Combine("UserData", "Configs");
        Directory.CreateDirectory(destDir);

        var opkFiles = Directory.GetFiles(sourceDir, "*.opk");
        var total = opkFiles.Length;

        for (var i = 0; i < opkFiles.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            var file = opkFiles[i];
            var fileName = Path.GetFileName(file);
            progress?.Report(($"Migrating configs... ({i + 1}/{total})", i + 1, total));

            try
            {
                var destPath = Path.Combine(destDir, fileName);
                if (File.Exists(destPath))
                {
                    result.ConfigsSkipped++;
                    continue;
                }

                File.Copy(file, destPath);
                result.ConfigsMigrated++;
                UploadFileInBackground(destPath, "config", Path.GetFileNameWithoutExtension(fileName));
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Config '{fileName}': {ex.Message}");
            }
        }
    }
    #endregion

    // ================================================================
    //  SQLite source
    // ================================================================
    #region SQLite Migration
    private async Task MigrateFromSqliteAsync(
        string dbPath, string ob2FolderPath, MigrationOptions options,
        MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        // Copy DB (+WAL/SHM) to temp dir so we can checkpoint without modifying the original
        progress?.Report(("Preparing database copy...", 0, 0));
        var tempDir = Path.Combine(Path.GetTempPath(), $"pb_migration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var tempDbPath = Path.Combine(tempDir, Path.GetFileName(dbPath));

        try
        {
            File.Copy(dbPath, tempDbPath, true);
            var walPath = dbPath + "-wal";
            var shmPath = dbPath + "-shm";
            if (File.Exists(walPath)) File.Copy(walPath, tempDbPath + "-wal", true);
            if (File.Exists(shmPath)) File.Copy(shmPath, tempDbPath + "-shm", true);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to copy database files: {ex.Message}");
            try { Directory.Delete(tempDir, true); } catch { }
            return;
        }

        try
        {
            // Open read-write to allow WAL checkpoint/recovery
            var connStr = $"Data Source={tempDbPath}";
            using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(ct);

            // Force WAL checkpoint to consolidate the database
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                await pragmaCmd.ExecuteNonQueryAsync(ct);
            }

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                tables.Add(reader.GetString(0));
        }

        // 2. Proxies
        var wordlistIdMap = new Dictionary<int, int>();

        if (options.MigrateProxies)
        {
            ct.ThrowIfCancellationRequested();
            await MigrateProxiesSqliteAsync(conn, tables, result, progress, ct);
        }

        // 3. Wordlists
        if (options.MigrateWordlists)
        {
            ct.ThrowIfCancellationRequested();
            wordlistIdMap = await MigrateWordlistsSqliteAsync(conn, tables, ob2FolderPath, result, progress, ct);
        }

        // 4. Jobs
        if (options.MigrateJobs)
        {
            ct.ThrowIfCancellationRequested();
            await MigrateJobsSqliteAsync(conn, tables, wordlistIdMap, result, progress, ct);
        }

        // 5. Hits + Records
        if (options.MigrateHits)
        {
            ct.ThrowIfCancellationRequested();
            await MigrateHitsSqliteAsync(conn, tables, wordlistIdMap, result, progress, ct);

            ct.ThrowIfCancellationRequested();
            await MigrateRecordsSqliteAsync(conn, tables, wordlistIdMap, result, progress, ct);
        }
        }
        finally
        {
            // Clean up temp files
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    private async Task MigrateProxiesSqliteAsync(
        SqliteConnection conn, HashSet<string> tables, MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        progress?.Report(("Migrating proxy groups...", 0, 0));

        var groupTable = FindTable(tables, "ProxyGroups", "ProxyGroupEntity");
        var proxyTable = FindTable(tables, "Proxies", "ProxyEntity");

        if (groupTable == null)
        {
            result.Errors.Add("Proxy groups table not found in SQLite database.");
            return;
        }

        // Read groups
        var groups = new List<(int id, string name)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT Id, Name FROM \"{groupTable}\"";
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                groups.Add((reader.GetInt32(0), reader.IsDBNull(1) ? $"Imported_{reader.GetInt32(0)}" : reader.GetString(1)));
            }
        }

        var total = groups.Count;
        for (var i = 0; i < groups.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var (oldGroupId, groupName) = groups[i];
            progress?.Report(($"Migrating proxy group '{groupName}'...", i + 1, total));

            try
            {
                var existing = await context.ProxyGroups.FirstOrDefaultAsync(g => g.Name == groupName, ct);
                if (existing != null)
                {
                    result.ProxyGroupsSkipped++;
                    continue;
                }

                var newGroup = new ProxyGroupEntity { Name = groupName, Owner = null };
                context.ProxyGroups.Add(newGroup);
                await context.SaveChangesAsync(ct);
                result.ProxyGroupsMigrated++;

                // Migrate proxies for this group
                if (proxyTable != null)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT Host, Port, Type, Username, Password, Country, Status, Ping, LastChecked FROM \"{proxyTable}\" WHERE GroupId = @gid";
                    cmd.Parameters.AddWithValue("@gid", oldGroupId);
                    using var reader = await cmd.ExecuteReaderAsync(ct);

                    var batch = new List<ProxyEntity>();
                    var allGroupProxies = new List<ProxyEntity>();
                    while (await reader.ReadAsync(ct))
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            var proxy = new ProxyEntity
                            {
                                Host = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                Port = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                Type = reader.IsDBNull(2) ? ProxyType.Http : ParseProxyTypeFromObj(reader.GetValue(2)),
                                Username = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Password = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Country = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Status = reader.IsDBNull(6) ? ProxyWorkingStatus.Untested : ParseProxyStatusFromObj(reader.GetValue(6)),
                                Ping = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                LastChecked = reader.IsDBNull(8) ? DateTime.MinValue : DateTime.Parse(reader.GetString(8)),
                                Group = newGroup
                            };
                            batch.Add(proxy);
                            allGroupProxies.Add(proxy);

                            if (batch.Count >= 500)
                            {
                                context.Proxies.AddRange(batch);
                                await context.SaveChangesAsync(ct);
                                result.ProxiesMigrated += batch.Count;
                                batch.Clear();
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Proxy in group '{groupName}': {ex.Message}");
                        }
                    }

                    if (batch.Count > 0)
                    {
                        context.Proxies.AddRange(batch);
                        await context.SaveChangesAsync(ct);
                        result.ProxiesMigrated += batch.Count;
                    }

                    UploadProxiesInBackground(allGroupProxies, groupName);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Proxy group '{groupName}': {ex.Message}");
            }
        }
    }

    private async Task<Dictionary<int, int>> MigrateWordlistsSqliteAsync(
        SqliteConnection conn, HashSet<string> tables, string ob2FolderPath,
        MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var idMap = new Dictionary<int, int>();
        var table = FindTable(tables, "Wordlists", "WordlistEntity");
        if (table == null)
        {
            result.Errors.Add("Wordlists table not found in SQLite database.");
            return idMap;
        }

        var destDir = Path.Combine("UserData", "Wordlists");
        Directory.CreateDirectory(destDir);

        var wordlists = new List<(int id, string name, string fileName, string purpose, int total, string type)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT Id, Name, FileName, Purpose, Total, Type FROM \"{table}\"";
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                wordlists.Add((
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? "" : reader.GetString(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                    reader.IsDBNull(3) ? "" : reader.GetString(3),
                    reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    reader.IsDBNull(5) ? "Default" : reader.GetString(5)
                ));
            }
        }

        var totalCount = wordlists.Count;
        for (var i = 0; i < wordlists.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var (oldId, name, oldFileName, purpose, lineTotal, wlType) = wordlists[i];
            progress?.Report(($"Migrating wordlist '{name}'... ({i + 1}/{totalCount})", i + 1, totalCount));

            try
            {
                var sourceFile = ResolveWordlistPath(oldFileName, ob2FolderPath);
                if (sourceFile == null || !File.Exists(sourceFile))
                {
                    result.Errors.Add($"Wordlist '{name}': Source file not found ({oldFileName})");
                    result.WordlistsSkipped++;
                    continue;
                }

                var existing = await context.Wordlists.FirstOrDefaultAsync(w => w.Name == name, ct);
                if (existing != null)
                {
                    idMap[oldId] = existing.Id;
                    result.WordlistsSkipped++;
                    continue;
                }

                var newPath = Path.Combine(destDir, $"{Guid.NewGuid()}.txt");
                newPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? newPath.Replace('/', '\\')
                    : newPath.Replace('\\', '/');

                File.Copy(sourceFile, newPath);

                var entity = new WordlistEntity
                {
                    Name = name,
                    FileName = newPath,
                    Purpose = purpose,
                    Total = lineTotal > 0 ? lineTotal : File.ReadLines(newPath).Count(),
                    Type = wlType,
                    Owner = null
                };

                context.Wordlists.Add(entity);
                await context.SaveChangesAsync(ct);
                idMap[oldId] = entity.Id;
                result.WordlistsMigrated++;
                UploadFileInBackground(newPath, "wordlist", name);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Wordlist '{name}': {ex.Message}");
            }
        }

        return idMap;
    }

    private async Task MigrateJobsSqliteAsync(
        SqliteConnection conn, HashSet<string> tables, Dictionary<int, int> wordlistIdMap,
        MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var table = FindTable(tables, "Jobs", "JobEntity");
        if (table == null)
        {
            result.Errors.Add("Jobs table not found in SQLite database.");
            return;
        }

        var jobs = new List<(DateTime creationDate, int jobType, string jobOptions)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT CreationDate, JobType, JobOptions FROM \"{table}\"";
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                jobs.Add((
                    reader.IsDBNull(0) ? DateTime.MinValue : DateTime.Parse(reader.GetString(0)),
                    reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)
                ));
            }
        }

        var total = jobs.Count;
        for (var i = 0; i < jobs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var (creationDate, jobTypeInt, jobOptions) = jobs[i];
            progress?.Report(($"Migrating jobs... ({i + 1}/{total})", i + 1, total));

            try
            {
                if (string.IsNullOrEmpty(jobOptions))
                    continue;

                // Namespace replacement
                jobOptions = jobOptions
                    .Replace("OpenBullet2.Core.Models", "ProjectBullet.Core.Models")
                    .Replace("OpenBullet2.Models", "ProjectBullet.Core.Models")
                    .Replace(", OpenBullet2.Core\"", ", ProjectBullet.Core\"")
                    .Replace(", OpenBullet2\"", ", ProjectBullet.Core\"");

                jobOptions = RemapWordlistIdInJson(jobOptions, wordlistIdMap);

                var entity = new JobEntity
                {
                    CreationDate = creationDate,
                    JobType = (JobType)jobTypeInt,
                    JobOptions = jobOptions,
                    Owner = null
                };

                context.Jobs.Add(entity);
                await context.SaveChangesAsync(ct);
                result.JobsMigrated++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Job #{i}: {ex.Message}");
            }
        }
    }

    private async Task MigrateHitsSqliteAsync(
        SqliteConnection conn, HashSet<string> tables, Dictionary<int, int> wordlistIdMap,
        MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var table = FindTable(tables, "Hits", "HitEntity");
        if (table == null)
        {
            result.Errors.Add("Hits table not found in SQLite database.");
            return;
        }

        // Get total count
        var totalCount = 0;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT COUNT(*) FROM \"{table}\"";
            totalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }

        progress?.Report(($"Migrating hits... (0/{totalCount})", 0, totalCount));

        context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Data, CapturedData, Proxy, Date, Type, ConfigId, ConfigName, ConfigCategory, WordlistId, WordlistName FROM \"{table}\"";
            using var reader = await cmd.ExecuteReaderAsync(ct);

            var batch = new List<HitEntity>();
            var processed = 0;

            while (await reader.ReadAsync(ct))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var oldWordlistId = reader.IsDBNull(8) ? -1 : reader.GetInt32(8);
                    var newWordlistId = wordlistIdMap.TryGetValue(oldWordlistId, out var mapped) ? mapped : oldWordlistId;

                    var entity = new HitEntity
                    {
                        Data = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                        CapturedData = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Proxy = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Date = reader.IsDBNull(3) ? DateTime.MinValue : DateTime.Parse(reader.GetString(3)),
                        Type = reader.IsDBNull(4) ? "SUCCESS" : reader.GetString(4),
                        ConfigId = reader.IsDBNull(5) ? null : reader.GetString(5),
                        ConfigName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        ConfigCategory = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        WordlistId = newWordlistId,
                        WordlistName = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                    };

                    batch.Add(entity);
                    processed++;

                    if (batch.Count >= 5000)
                    {
                        context.Hits.AddRange(batch);
                        await context.SaveChangesAsync(ct);
                        result.HitsMigrated += batch.Count;
                        batch.Clear();
                        progress?.Report(($"Migrating hits... ({processed}/{totalCount})", processed, totalCount));
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Hit: {ex.Message}");
                }
            }

            if (batch.Count > 0)
            {
                context.Hits.AddRange(batch);
                await context.SaveChangesAsync(ct);
                result.HitsMigrated += batch.Count;
            }
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    private async Task MigrateRecordsSqliteAsync(
        SqliteConnection conn, HashSet<string> tables, Dictionary<int, int> wordlistIdMap,
        MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var table = FindTable(tables, "Records", "RecordEntity");
        if (table == null)
            return;

        var records = new List<(string configId, int wordlistId, int checkpoint)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT ConfigId, WordlistId, Checkpoint FROM \"{table}\"";
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                records.Add((
                    reader.IsDBNull(0) ? null : reader.GetString(0),
                    reader.IsDBNull(1) ? -1 : reader.GetInt32(1),
                    reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
                ));
            }
        }

        progress?.Report(("Migrating records...", 0, records.Count));

        for (var i = 0; i < records.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var (configId, oldWordlistId, checkpoint) = records[i];

            try
            {
                var newWordlistId = wordlistIdMap.TryGetValue(oldWordlistId, out var mapped) ? mapped : oldWordlistId;

                var exists = await context.Records.AnyAsync(
                    r => r.ConfigId == configId && r.WordlistId == newWordlistId, ct);
                if (exists)
                    continue;

                context.Records.Add(new RecordEntity
                {
                    ConfigId = configId,
                    WordlistId = newWordlistId,
                    Checkpoint = checkpoint
                });
                await context.SaveChangesAsync(ct);
                result.RecordsMigrated++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Record: {ex.Message}");
            }
        }
    }
    #endregion

    // ================================================================
    //  LiteDB source
    // ================================================================
    #region LiteDB Migration
    private async Task MigrateFromLiteDbAsync(
        string dbPath, string ob2FolderPath, MigrationOptions options,
        MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var connectionString = $"Filename={dbPath};ReadOnly=true;Connection=shared";
        using var liteDb = new LiteDatabase(connectionString);

        var wordlistIdMap = new Dictionary<int, int>();

        if (options.MigrateProxies)
        {
            ct.ThrowIfCancellationRequested();
            await MigrateProxiesLiteDbAsync(liteDb, result, progress, ct);
        }

        if (options.MigrateWordlists)
        {
            ct.ThrowIfCancellationRequested();
            wordlistIdMap = await MigrateWordlistsLiteDbAsync(liteDb, ob2FolderPath, result, progress, ct);
        }

        if (options.MigrateJobs)
        {
            ct.ThrowIfCancellationRequested();
            await MigrateJobsLiteDbAsync(liteDb, wordlistIdMap, result, progress, ct);
        }

        if (options.MigrateHits)
        {
            ct.ThrowIfCancellationRequested();
            await MigrateHitsLiteDbAsync(liteDb, wordlistIdMap, result, progress, ct);

            ct.ThrowIfCancellationRequested();
            await MigrateRecordsLiteDbAsync(liteDb, wordlistIdMap, result, progress, ct);
        }
    }

    private async Task MigrateProxiesLiteDbAsync(
        LiteDatabase liteDb, MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        progress?.Report(("Migrating proxy groups...", 0, 0));

        var groupCollection = FindLiteCollection(liteDb, "ProxyGroupEntity", "ProxyGroup", "proxygroups", "ProxyGroups");
        if (groupCollection == null)
        {
            result.Errors.Add("Proxy groups collection not found in LiteDB.");
            return;
        }

        var proxyCollection = FindLiteCollection(liteDb, "ProxyEntity", "Proxy", "proxies", "Proxies");
        var groups = groupCollection.FindAll().ToList();
        var total = groups.Count;

        for (var i = 0; i < groups.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var groupDoc = groups[i];
            var groupName = groupDoc["Name"]?.AsString ?? $"Imported_{i}";
            progress?.Report(($"Migrating proxy group '{groupName}'...", i + 1, total));

            try
            {
                var existing = await context.ProxyGroups.FirstOrDefaultAsync(g => g.Name == groupName, ct);
                if (existing != null)
                {
                    result.ProxyGroupsSkipped++;
                    continue;
                }

                var newGroup = new ProxyGroupEntity { Name = groupName, Owner = null };
                context.ProxyGroups.Add(newGroup);
                await context.SaveChangesAsync(ct);
                result.ProxyGroupsMigrated++;

                if (proxyCollection != null)
                {
                    var oldGroupId = groupDoc["_id"];
                    var proxies = proxyCollection.Find(Query.EQ("GroupId", oldGroupId))
                        .Concat(proxyCollection.Find(Query.EQ("Group.$id", oldGroupId)))
                        .ToList();

                    if (!proxies.Any() && groupDoc.ContainsKey("Proxies") && groupDoc["Proxies"].IsArray)
                        proxies = groupDoc["Proxies"].AsArray.Select(p => p.AsDocument).ToList();

                    var batch = new List<ProxyEntity>();
                    var allGroupProxies = new List<ProxyEntity>();
                    foreach (var proxyDoc in proxies)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            var entity = new ProxyEntity
                            {
                                Host = proxyDoc["Host"]?.AsString ?? string.Empty,
                                Port = proxyDoc["Port"]?.AsInt32 ?? 0,
                                Type = ParseProxyTypeBson(proxyDoc["Type"]),
                                Username = proxyDoc["Username"]?.AsString ?? string.Empty,
                                Password = proxyDoc["Password"]?.AsString ?? string.Empty,
                                Country = proxyDoc["Country"]?.AsString ?? string.Empty,
                                Status = ParseProxyStatusBson(proxyDoc["Status"]),
                                Ping = proxyDoc["Ping"]?.AsInt32 ?? 0,
                                LastChecked = GetDateTimeBson(proxyDoc, "LastChecked"),
                                Group = newGroup
                            };
                            batch.Add(entity);
                            allGroupProxies.Add(entity);

                            if (batch.Count >= 500)
                            {
                                context.Proxies.AddRange(batch);
                                await context.SaveChangesAsync(ct);
                                result.ProxiesMigrated += batch.Count;
                                batch.Clear();
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Proxy in group '{groupName}': {ex.Message}");
                        }
                    }

                    if (batch.Count > 0)
                    {
                        context.Proxies.AddRange(batch);
                        await context.SaveChangesAsync(ct);
                        result.ProxiesMigrated += batch.Count;
                    }

                    UploadProxiesInBackground(allGroupProxies, groupName);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Proxy group '{groupName}': {ex.Message}");
            }
        }
    }

    private async Task<Dictionary<int, int>> MigrateWordlistsLiteDbAsync(
        LiteDatabase liteDb, string ob2FolderPath, MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var idMap = new Dictionary<int, int>();
        var collection = FindLiteCollection(liteDb, "WordlistEntity", "Wordlist", "wordlists", "Wordlists");
        if (collection == null)
        {
            result.Errors.Add("Wordlists collection not found in LiteDB.");
            return idMap;
        }

        var destDir = Path.Combine("UserData", "Wordlists");
        Directory.CreateDirectory(destDir);

        var wordlists = collection.FindAll().ToList();
        var total = wordlists.Count;

        for (var i = 0; i < wordlists.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var doc = wordlists[i];
            var name = doc["Name"]?.AsString ?? $"Imported_{i}";
            progress?.Report(($"Migrating wordlist '{name}'... ({i + 1}/{total})", i + 1, total));

            try
            {
                var oldId = doc["_id"].AsInt32;
                var oldFileName = doc["FileName"]?.AsString ?? string.Empty;

                var sourceFile = ResolveWordlistPath(oldFileName, ob2FolderPath);
                if (sourceFile == null || !File.Exists(sourceFile))
                {
                    result.Errors.Add($"Wordlist '{name}': Source file not found ({oldFileName})");
                    result.WordlistsSkipped++;
                    continue;
                }

                var existing = await context.Wordlists.FirstOrDefaultAsync(w => w.Name == name, ct);
                if (existing != null)
                {
                    idMap[oldId] = existing.Id;
                    result.WordlistsSkipped++;
                    continue;
                }

                var newPath = Path.Combine(destDir, $"{Guid.NewGuid()}.txt");
                newPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? newPath.Replace('/', '\\') : newPath.Replace('\\', '/');

                File.Copy(sourceFile, newPath);

                var entity = new WordlistEntity
                {
                    Name = name,
                    FileName = newPath,
                    Purpose = doc["Purpose"]?.AsString ?? string.Empty,
                    Total = doc["Total"]?.AsInt32 ?? File.ReadLines(newPath).Count(),
                    Type = doc["Type"]?.AsString ?? "Default",
                    Owner = null
                };

                context.Wordlists.Add(entity);
                await context.SaveChangesAsync(ct);
                idMap[oldId] = entity.Id;
                result.WordlistsMigrated++;
                UploadFileInBackground(newPath, "wordlist", name);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Wordlist '{name}': {ex.Message}");
            }
        }

        return idMap;
    }

    private async Task MigrateJobsLiteDbAsync(
        LiteDatabase liteDb, Dictionary<int, int> wordlistIdMap, MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var collection = FindLiteCollection(liteDb, "JobEntity", "Job", "jobs", "Jobs");
        if (collection == null)
        {
            result.Errors.Add("Jobs collection not found in LiteDB.");
            return;
        }

        var jobs = collection.FindAll().ToList();
        var total = jobs.Count;

        for (var i = 0; i < jobs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var doc = jobs[i];
            progress?.Report(($"Migrating jobs... ({i + 1}/{total})", i + 1, total));

            try
            {
                var jobOptions = doc["JobOptions"]?.AsString;
                if (string.IsNullOrEmpty(jobOptions)) continue;

                jobOptions = jobOptions
                    .Replace("OpenBullet2.Core.Models", "ProjectBullet.Core.Models")
                    .Replace("OpenBullet2.Models", "ProjectBullet.Core.Models")
                    .Replace(", OpenBullet2.Core\"", ", ProjectBullet.Core\"")
                    .Replace(", OpenBullet2\"", ", ProjectBullet.Core\"");

                jobOptions = RemapWordlistIdInJson(jobOptions, wordlistIdMap);

                var jobType = JobType.MultiRun;
                if (doc.ContainsKey("JobType"))
                {
                    var typeVal = doc["JobType"];
                    if (typeVal.IsInt32) jobType = (JobType)typeVal.AsInt32;
                    else if (typeVal.IsString && Enum.TryParse<JobType>(typeVal.AsString, out var parsed))
                        jobType = parsed;
                }

                context.Jobs.Add(new JobEntity
                {
                    CreationDate = GetDateTimeBson(doc, "CreationDate"),
                    JobType = jobType,
                    JobOptions = jobOptions,
                    Owner = null
                });
                await context.SaveChangesAsync(ct);
                result.JobsMigrated++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Job #{i}: {ex.Message}");
            }
        }
    }

    private async Task MigrateHitsLiteDbAsync(
        LiteDatabase liteDb, Dictionary<int, int> wordlistIdMap, MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var collection = FindLiteCollection(liteDb, "HitEntity", "Hit", "hits", "Hits");
        if (collection == null)
        {
            result.Errors.Add("Hits collection not found in LiteDB.");
            return;
        }

        var totalCount = collection.Count();
        progress?.Report(($"Migrating hits... (0/{totalCount})", 0, totalCount));

        context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            var batch = new List<HitEntity>();
            var processed = 0;

            foreach (var doc in collection.FindAll())
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var oldWlId = doc["WordlistId"]?.AsInt32 ?? -1;
                    batch.Add(new HitEntity
                    {
                        Data = doc["Data"]?.AsString ?? string.Empty,
                        CapturedData = doc["CapturedData"]?.AsString ?? string.Empty,
                        Proxy = doc["Proxy"]?.AsString ?? string.Empty,
                        Date = GetDateTimeBson(doc, "Date"),
                        Type = doc["Type"]?.AsString ?? "SUCCESS",
                        ConfigId = doc["ConfigId"]?.AsString,
                        ConfigName = doc["ConfigName"]?.AsString ?? string.Empty,
                        ConfigCategory = doc["ConfigCategory"]?.AsString ?? string.Empty,
                        WordlistId = wordlistIdMap.TryGetValue(oldWlId, out var m) ? m : oldWlId,
                        WordlistName = doc["WordlistName"]?.AsString ?? string.Empty
                    });
                    processed++;

                    if (batch.Count >= 5000)
                    {
                        context.Hits.AddRange(batch);
                        await context.SaveChangesAsync(ct);
                        result.HitsMigrated += batch.Count;
                        batch.Clear();
                        progress?.Report(($"Migrating hits... ({processed}/{totalCount})", processed, totalCount));
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Hit: {ex.Message}");
                }
            }

            if (batch.Count > 0)
            {
                context.Hits.AddRange(batch);
                await context.SaveChangesAsync(ct);
                result.HitsMigrated += batch.Count;
            }
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    private async Task MigrateRecordsLiteDbAsync(
        LiteDatabase liteDb, Dictionary<int, int> wordlistIdMap, MigrationResult result,
        IProgress<(string status, int current, int total)> progress,
        CancellationToken ct)
    {
        var collection = FindLiteCollection(liteDb, "RecordEntity", "Record", "records", "Records");
        if (collection == null) return;

        var records = collection.FindAll().ToList();
        progress?.Report(("Migrating records...", 0, records.Count));

        for (var i = 0; i < records.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var doc = records[i];

            try
            {
                var configId = doc["ConfigId"]?.AsString;
                var oldWlId = doc["WordlistId"]?.AsInt32 ?? -1;
                var newWlId = wordlistIdMap.TryGetValue(oldWlId, out var m) ? m : oldWlId;

                if (await context.Records.AnyAsync(r => r.ConfigId == configId && r.WordlistId == newWlId, ct))
                    continue;

                context.Records.Add(new RecordEntity
                {
                    ConfigId = configId,
                    WordlistId = newWlId,
                    Checkpoint = doc["Checkpoint"]?.AsInt32 ?? 0
                });
                await context.SaveChangesAsync(ct);
                result.RecordsMigrated++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Record: {ex.Message}");
            }
        }
    }
    #endregion

    // ================================================================
    //  Shared helpers
    // ================================================================
    #region Helpers

    private static string FindTable(HashSet<string> tables, params string[] candidates)
    {
        foreach (var name in candidates)
        {
            if (tables.Contains(name))
                return name;
        }
        // Case-insensitive fallback
        foreach (var name in candidates)
        {
            var match = tables.FirstOrDefault(t => t.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }
        return null;
    }

    private static ILiteCollection<BsonDocument> FindLiteCollection(LiteDatabase db, params string[] names)
    {
        var collectionNames = db.GetCollectionNames().ToList();
        foreach (var name in names)
        {
            var match = collectionNames.FirstOrDefault(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match != null) return db.GetCollection(match);
        }
        return null;
    }

    private static ProxyType ParseProxyTypeFromObj(object value)
    {
        if (value is long l) return (ProxyType)(int)l;
        if (value is int i) return (ProxyType)i;
        if (value is string s && Enum.TryParse<ProxyType>(s, true, out var p)) return p;
        return ProxyType.Http;
    }

    private static ProxyWorkingStatus ParseProxyStatusFromObj(object value)
    {
        if (value is long l) return (ProxyWorkingStatus)(int)l;
        if (value is int i) return (ProxyWorkingStatus)i;
        if (value is string s && Enum.TryParse<ProxyWorkingStatus>(s, true, out var p)) return p;
        return ProxyWorkingStatus.Untested;
    }

    private static ProxyType ParseProxyTypeBson(BsonValue value)
    {
        if (value == null || value.IsNull) return ProxyType.Http;
        if (value.IsInt32) return (ProxyType)value.AsInt32;
        if (value.IsString && Enum.TryParse<ProxyType>(value.AsString, true, out var p)) return p;
        return ProxyType.Http;
    }

    private static ProxyWorkingStatus ParseProxyStatusBson(BsonValue value)
    {
        if (value == null || value.IsNull) return ProxyWorkingStatus.Untested;
        if (value.IsInt32) return (ProxyWorkingStatus)value.AsInt32;
        if (value.IsString && Enum.TryParse<ProxyWorkingStatus>(value.AsString, true, out var p)) return p;
        return ProxyWorkingStatus.Untested;
    }

    private static DateTime GetDateTimeBson(BsonDocument doc, string field)
    {
        if (!doc.ContainsKey(field) || doc[field].IsNull) return DateTime.MinValue;
        var val = doc[field];
        if (val.IsDateTime) return val.AsDateTime;
        if (val.IsString && DateTime.TryParse(val.AsString, out var dt)) return dt;
        return DateTime.MinValue;
    }

    private static string ResolveWordlistPath(string filePath, string ob2FolderPath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return null;

        if (File.Exists(filePath)) return filePath;

        var relative = Path.Combine(ob2FolderPath, filePath);
        if (File.Exists(relative)) return relative;

        var fileName = Path.GetFileName(filePath);
        var inWordlists = Path.Combine(ob2FolderPath, "UserData", "Wordlists", fileName);
        if (File.Exists(inWordlists)) return inWordlists;

        return null;
    }

    private static string RemapWordlistIdInJson(string json, Dictionary<int, int> idMap)
    {
        if (idMap.Count == 0) return json;
        try
        {
            var obj = JObject.Parse(json);
            RemapWordlistIds(obj, idMap);
            return obj.ToString(Newtonsoft.Json.Formatting.None);
        }
        catch { return json; }
    }

    private static void RemapWordlistIds(JToken token, Dictionary<int, int> idMap)
    {
        switch (token)
        {
            case JObject obj:
                if (obj.TryGetValue("WordlistId", out var wlId) && wlId.Type == JTokenType.Integer)
                {
                    var oldId = wlId.Value<int>();
                    if (idMap.TryGetValue(oldId, out var newId))
                        obj["WordlistId"] = newId;
                }
                foreach (var prop in obj.Properties())
                    RemapWordlistIds(prop.Value, idMap);
                break;
            case JArray arr:
                foreach (var item in arr)
                    RemapWordlistIds(item, idMap);
                break;
        }
    }
    #endregion
}
