using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Trove.Core.Settings;
using Trove.Web.Data;

namespace Trove.Web.YouTube;

/// <summary>
/// Finds the YouTube API key to use: one saved on the Settings screen wins, otherwise <c>YouTube:ApiKey</c> from user
/// secrets or the environment. A saved key is encrypted with ASP.NET Core Data Protection before it reaches the
/// database, so a copy of CORA's Postgres alone doesn't give it away.
/// </summary>
public sealed class YouTubeKeyStore(
    IServiceScopeFactory scopes, IOptionsMonitor<YouTubeOptions> options, IDataProtectionProvider protection,
    TimeProvider time, ILogger<YouTubeKeyStore> logger)
{
    public const string SettingKey = "youtube.api_key";

    private readonly IDataProtector protector = protection.CreateProtector("Trove.YouTube.ApiKey");
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? saved;
    private bool loaded;

    /// <summary>True when the saved key couldn't be decrypted (the Data Protection keys were lost), so it needs entering again.</summary>
    public bool SavedKeyUnreadable { get; private set; }

    public async Task<(string? Key, YouTubeKeySource Source)> GetAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(saved))
        {
            return (saved, YouTubeKeySource.Saved);
        }

        var configured = options.CurrentValue.ApiKey;
        return string.IsNullOrWhiteSpace(configured) ? (null, YouTubeKeySource.None) : (configured.Trim(), YouTubeKeySource.Configuration);
    }

    public async Task<bool> HasKeyAsync(CancellationToken cancellationToken = default) =>
        (await GetAsync(cancellationToken)).Key is not null;

    public async Task SaveAsync(string key, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TroveDbContext>();
            var row = await db.AppSettings.FindAsync([SettingKey], cancellationToken);
            var value = protector.Protect(key.Trim());
            if (row is null)
            {
                db.AppSettings.Add(new AppSettingRow { Key = SettingKey, Value = value, UpdatedAt = time.GetUtcNow() });
            }
            else
            {
                row.Value = value;
                row.UpdatedAt = time.GetUtcNow();
            }

            await db.SaveChangesAsync(cancellationToken);
            saved = key.Trim();
            loaded = true;
            SavedKeyUnreadable = false;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task RemoveAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TroveDbContext>();
            await db.AppSettings.Where(s => s.Key == SettingKey).ExecuteDeleteAsync(cancellationToken);
            saved = null;
            loaded = true;
            SavedKeyUnreadable = false;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (loaded)
        {
            return;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (loaded)
            {
                return;
            }

            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TroveDbContext>();
            var row = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == SettingKey, cancellationToken);
            if (row is not null)
            {
                try
                {
                    saved = protector.Unprotect(row.Value);
                }
                catch (CryptographicException)
                {
                    logger.LogWarning("The saved YouTube API key can't be decrypted; enter it again on the Settings screen.");
                    SavedKeyUnreadable = true;
                }
            }

            loaded = true;
        }
        finally
        {
            gate.Release();
        }
    }
}
