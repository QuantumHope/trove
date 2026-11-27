using Trove.Core.Setup;

namespace Trove.UI.Setup;

/// <summary>What the module is missing, and the one setting Trove can save itself: CORA's address.</summary>
public interface ITroveSetup
{
    Task<SetupStatus> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves CORA's address after the module has checked CORA answers there. Not saved comes back with the reason;
    /// <see cref="UnauthorizedAccessException"/> when the person signed in isn't CORA's admin.
    /// </summary>
    Task<SaveCoraAddressResult> SaveCoraAddressAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>Raised after a save, so a page showing the setup can read it again.</summary>
    event Action? Changed;
}
