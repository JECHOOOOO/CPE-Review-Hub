(() => {
    const overlay = document.querySelector("[data-privacy-overlay]");
    const message = document.querySelector("[data-privacy-message]");
    if (!overlay) return;
    let shortcutTimer;
    const cover = (text = "Return to COLMS to continue.") => {
        if (message) message.textContent = text;
        overlay.hidden = false;
    };
    // Discourage casual copying/saving on protected lesson, activity, and
    // assessment pages. This is only a browser-side deterrent; it is not DRM.
    document.addEventListener("contextmenu", (event) => event.preventDefault());
    document.addEventListener("dragstart", (event) => {
        if (event.target instanceof Element && event.target.closest("img, .protected-learning-content")) {
            event.preventDefault();
        }
    });
    const reveal = () => {
        if (!document.hidden) overlay.hidden = true;
    };
    document.addEventListener("visibilitychange", () => document.hidden ? cover() : reveal());
    window.addEventListener("blur", () => cover());
    window.addEventListener("focus", reveal);
    document.addEventListener("keydown", (event) => {
        if (event.key === "PrintScreen") {
            cover("Protected content is covered. Browser screenshot blocking is limited.");
            clearTimeout(shortcutTimer);
            shortcutTimer = setTimeout(reveal, 1800);
        }
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "p") {
            event.preventDefault();
            cover("Printing of protected COLMS learning content is disabled.");
            clearTimeout(shortcutTimer);
            shortcutTimer = setTimeout(reveal, 2200);
        }
    });
    window.addEventListener("beforeprint", () => cover("Printing of protected COLMS learning content is disabled."));
})();
