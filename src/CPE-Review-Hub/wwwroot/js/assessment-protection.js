(() => {
    const assessmentPage = document.body.classList.contains("assessment-taking-page");
    if (!assessmentPage) return;

    const protectedContent = ".assessment-protected";
    const isProtectedTarget = target =>
        target instanceof Element && target.closest(protectedContent) !== null;
    const isProtectedSelection = () => {
        const selection = document.getSelection();
        const isProtectedNode = node => {
            const element = node instanceof Element ? node : node?.parentElement;
            return element instanceof Element && element.closest(protectedContent) !== null;
        };
        return selection && !selection.isCollapsed &&
            (isProtectedNode(selection.anchorNode) || isProtectedNode(selection.focusNode));
    };
    const originatesInAssessment = event =>
        isProtectedTarget(event.target) || isProtectedSelection();

    for (const image of document.querySelectorAll(`${protectedContent} img`)) {
        image.draggable = false;
    }

    document.addEventListener("copy", event => {
        if (originatesInAssessment(event)) event.preventDefault();
    });
    document.addEventListener("cut", event => {
        if (originatesInAssessment(event)) event.preventDefault();
    });
    document.addEventListener("contextmenu", event => {
        if (isProtectedTarget(event.target)) event.preventDefault();
    });
    document.addEventListener("dragstart", event => {
        if (isProtectedTarget(event.target)) event.preventDefault();
    });

    document.addEventListener("keydown", event => {
        if (!(event.ctrlKey || event.metaKey)) return;
        const key = event.key.toLowerCase();
        if (key === "p" || ((key === "c" || key === "x") && originatesInAssessment(event))) {
            event.preventDefault();
        }
    });
})();
