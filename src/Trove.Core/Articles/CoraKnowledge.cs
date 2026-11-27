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

/// <summary>What the card's CORA row offers: one action, which either asks CORA to read the page or opens it in CORA.</summary>
public enum CoraAction
{
    /// <summary>Nothing to do yet: a feed import is bringing it in.</summary>
    None,

    /// <summary>Ask CORA to read the page.</summary>
    Add,

    /// <summary>Ask CORA to read the page again after it failed.</summary>
    Retry,

    /// <summary>Open the text CORA pulled, where you check it and press Analyze.</summary>
    CheckText,

    /// <summary>Open the changeset while CORA analyzes it.</summary>
    ViewProgress,

    /// <summary>Open the changeset whose proposals wait for your review.</summary>
    Review,

    /// <summary>Open the document in CORA's library.</summary>
    View,
}

/// <summary>Whether CORA has read an article, and the changeset in CORA that did.</summary>
/// <param name="ChangesetStatus">That changeset's own status (unread, pending, drafting, open, applied, failed), which says what's next.</param>
public sealed record CoraKnowledge(KnowledgeState State, string? ChangesetId = null, string? ChangesetStatus = null)
{
    /// <summary>The badge on the card: where the page is in CORA, in a few words.</summary>
    public string Label => (State, ChangesetStatus) switch
    {
        (KnowledgeState.InCora, _) => "In CORA",
        (KnowledgeState.Queued, "unread") => "Needs your check",
        (KnowledgeState.Queued, "pending" or "drafting") => "CORA is reading",
        (KnowledgeState.Queued, "open") => "Ready to review",
        (KnowledgeState.Queued, _) => "On its way to CORA",
        (KnowledgeState.Failed, _) => "CORA couldn't read it",
        _ => "Not in CORA",
    };

    /// <summary>One short line under the badge on what that means for you.</summary>
    public string Detail => (State, ChangesetStatus) switch
    {
        (KnowledgeState.InCora, _) => "What CORA read is in your knowledge.",
        (KnowledgeState.Queued, "unread") => "Check the text CORA pulled, then press Analyze.",
        (KnowledgeState.Queued, "pending" or "drafting") => "CORA is analyzing the page.",
        (KnowledgeState.Queued, "open") => "CORA's proposals from it are waiting for you.",
        (KnowledgeState.Queued, _) => "A feed import is bringing it in.",
        (KnowledgeState.Failed, _) => "The last try didn't work.",
        _ => "CORA hasn't read this page yet.",
    };

    /// <summary>The one action the row offers.</summary>
    public CoraAction Action => (State, ChangesetStatus, ChangesetId) switch
    {
        (KnowledgeState.NotInCora, _, _) => CoraAction.Add,
        (KnowledgeState.Failed, _, _) => CoraAction.Retry,
        (_, _, null) => CoraAction.None,
        (KnowledgeState.InCora, _, _) => CoraAction.View,
        (KnowledgeState.Queued, "unread", _) => CoraAction.CheckText,
        (KnowledgeState.Queued, "open", _) => CoraAction.Review,
        _ => CoraAction.ViewProgress,
    };

    /// <summary>Whether the action asks CORA to read the page, rather than opening it in CORA.</summary>
    public bool CanAdd => Action is CoraAction.Add or CoraAction.Retry;

    /// <summary>Whether the row should call attention: something waits on you, or the read failed.</summary>
    public bool NeedsYou => Action is CoraAction.CheckText or CoraAction.Review or CoraAction.Retry;
}
