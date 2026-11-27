using System.Text.Json.Serialization;

namespace Trove.Core.Articles;

/// <summary>Where CORA's knowledge stands on an article's page.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<KnowledgeState>))]
public enum KnowledgeState
{
    /// <summary>CORA hasn't read it.</summary>
    NotInCora,

    /// <summary>CORA has it and nothing it found is accepted yet, or a feed import is on its way.</summary>
    Queued,

    /// <summary>What CORA read from it is in your knowledge.</summary>
    InCora,

    /// <summary>CORA's last try to read it failed.</summary>
    Failed,
}

/// <summary>Whether CORA has read an article, and the changeset in CORA that did.</summary>
/// <param name="ChangesetStatus">That changeset's own status (unread, pending, drafting, open, applied, failed), which says what's next.</param>
public sealed record CoraKnowledge(KnowledgeState State, string? ChangesetId = null, string? ChangesetStatus = null)
{
    /// <summary>The badge on the card.</summary>
    public string Label => State switch
    {
        KnowledgeState.InCora => "In CORA",
        KnowledgeState.Queued => "Queued in CORA",
        KnowledgeState.Failed => "CORA couldn't read it",
        _ => "Not in CORA",
    };

    /// <summary>What's next for a queued article, in a few words; null when the badge says enough.</summary>
    public string? Next => (State, ChangesetStatus) switch
    {
        (KnowledgeState.Queued, "unread") => "check its text, then Analyze",
        (KnowledgeState.Queued, "pending" or "drafting") => "CORA is reading it",
        (KnowledgeState.Queued, "open") => "ready to review",
        (KnowledgeState.Queued, null) => "a feed import is on its way",
        _ => null,
    };

    /// <summary>Whether Add to CORA makes sense: nothing was read, or the last try failed.</summary>
    public bool CanAdd => State is KnowledgeState.NotInCora or KnowledgeState.Failed;
}
