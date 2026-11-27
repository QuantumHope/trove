// YouTube's player, loaded only when someone presses Play (ADR-132 in CORA). It runs on youtube-nocookie.com, so
// YouTube sets no tracking cookie until the video plays. Trove tells .NET when the video starts, ends or can't play,
// and keeps a resume point in this browser so a long playthrough picks up where it stopped.

let apiReady;

function loadApi() {
    if (window.YT && window.YT.Player) {
        return Promise.resolve();
    }

    if (!apiReady) {
        apiReady = new Promise((resolve, reject) => {
            const previous = window.onYouTubeIframeAPIReady;
            window.onYouTubeIframeAPIReady = () => {
                if (typeof previous === "function") {
                    previous();
                }
                resolve();
            };
            const script = document.createElement("script");
            script.src = "https://www.youtube.com/iframe_api";
            script.async = true;
            script.onerror = () => {
                apiReady = undefined;
                reject(new Error("YouTube's player didn't load."));
            };
            document.head.appendChild(script);
        });
    }

    return apiReady;
}

const key = id => `trove.resume.${id}`;

/** Seconds saved for this video, or 0. */
export function resumeAt(videoId) {
    try {
        const saved = Number(localStorage.getItem(key(videoId)));
        return Number.isFinite(saved) && saved > 0 ? Math.floor(saved) : 0;
    } catch {
        return 0;
    }
}

function save(videoId, seconds) {
    try {
        if (seconds > 5) {
            localStorage.setItem(key(videoId), String(Math.floor(seconds)));
        }
    } catch {
        // Private windows can refuse storage; resuming is a nicety.
    }
}

export function forget(videoId) {
    try {
        localStorage.removeItem(key(videoId));
    } catch {
    }
}

/**
 * Builds the player inside `host` and starts it at `startAt` seconds, the spot the page offered. `dotnet` gets
 * OnPlaying(), OnEnded() and OnPlayerError(code). Returns a handle whose finish() forgets the saved spot for good
 * and whose dispose() saves the position, unless the video finished, and tears the player down.
 */
export async function mount(host, videoId, startAt, dotnet) {
    await loadApi();

    const target = document.createElement("div");
    host.replaceChildren(target);
    let timer;
    let gone = false;
    let finished = false;

    const player = new window.YT.Player(target, {
        host: "https://www.youtube-nocookie.com",
        videoId,
        width: "100%",
        height: "100%",
        playerVars: {
            autoplay: 1,
            start: startAt,
            rel: 0,
            playsinline: 1,
            origin: window.location.origin,
        },
        events: {
            onStateChange: e => {
                if (gone) {
                    return;
                }
                const s = window.YT.PlayerState;
                if (e.data === s.PLAYING) {
                    finished = false;
                    dotnet.invokeMethodAsync("OnPlaying");
                    clearInterval(timer);
                    timer = setInterval(() => save(videoId, player.getCurrentTime()), 10000);
                } else if (e.data === s.PAUSED) {
                    clearInterval(timer);
                    save(videoId, player.getCurrentTime());
                } else if (e.data === s.ENDED) {
                    clearInterval(timer);
                    finished = true;
                    forget(videoId);
                    dotnet.invokeMethodAsync("OnEnded");
                }
            },
            onError: e => {
                if (!gone) {
                    dotnet.invokeMethodAsync("OnPlayerError", e.data);
                }
            },
        },
    });

    return {
        finish() {
            finished = true;
            clearInterval(timer);
            forget(videoId);
        },
        dispose() {
            gone = true;
            clearInterval(timer);
            try {
                if (!finished && player.getCurrentTime) {
                    save(videoId, player.getCurrentTime());
                }
                player.destroy();
            } catch {
            }
        },
    };
}
