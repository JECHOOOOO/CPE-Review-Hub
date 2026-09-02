(() => {
    const form = document.querySelector('[data-assessment-form]');
    const count = document.querySelector('#answered-count');
    const progress = document.querySelector('#answered-progress');
    if (!form || !count || !progress) return;

    const questionNames = [...new Set([...form.querySelectorAll('input[type="radio"]')]
        .map(input => input.name))];
    const update = () => {
        const answered = questionNames.filter(name =>
            form.querySelector(`input[name="${CSS.escape(name)}"]:checked`)).length;
        count.textContent = `${answered} of ${questionNames.length} answered`;
        progress.style.width = `${questionNames.length ? (answered / questionNames.length) * 100 : 0}%`;
    };
    form.addEventListener('change', update);
    update();
})();
