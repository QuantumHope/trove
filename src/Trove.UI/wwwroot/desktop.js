// The Trove desktop window adds "TroveDesktop/<version>" to its user agent, so the page can tell it's inside the
// window from its first moment (a script the window injects later would race Blazor's start).
export function shell() {
    const match = /\bTroveDesktop\/([\w.+-]+)/.exec(navigator.userAgent);
    return match ? { version: match[1], origin: location.origin } : null;
}
