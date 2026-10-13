(() => {
    const backgroundVideo = document.querySelector('.auth-video');
    const playbackButton = document.querySelector('[data-background-playback]');
    if (backgroundVideo && playbackButton) {
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
        const updateLabel = () => {
            playbackButton.textContent = backgroundVideo.paused ? 'Play background' : 'Pause background';
        };
        const applyMotionPreference = () => {
            if (reducedMotion.matches) {
                backgroundVideo.autoplay = false;
                backgroundVideo.pause();
            } else {
                backgroundVideo.play().catch(updateLabel);
            }
            updateLabel();
        };
        playbackButton.hidden = false;
        backgroundVideo.addEventListener('play', updateLabel);
        backgroundVideo.addEventListener('pause', updateLabel);
        backgroundVideo.addEventListener('error', () => { playbackButton.hidden = true; });
        playbackButton.addEventListener('click', () => {
            if (backgroundVideo.paused) backgroundVideo.play().catch(updateLabel);
            else backgroundVideo.pause();
        });
        reducedMotion.addEventListener('change', applyMotionPreference);
        applyMotionPreference();
    }

    const video = document.querySelector('[data-crossfade-video]');
    if (video) {
        let frame = 0;
        let fadingOut = false;

        const fadeTo = (target, duration = 500) => {
            cancelAnimationFrame(frame);
            const start = performance.now();
            const initial = Number.parseFloat(video.style.opacity || '0');
            const animate = now => {
                const progress = Math.min((now - start) / duration, 1);
                video.style.opacity = String(initial + ((target - initial) * progress));
                if (progress < 1) frame = requestAnimationFrame(animate);
            };
            frame = requestAnimationFrame(animate);
        };

        video.addEventListener('canplay', () => {
            video.play().catch(() => {});
            fadeTo(1);
        }, { once: true });

        video.addEventListener('timeupdate', () => {
            if (!fadingOut && video.duration && video.duration - video.currentTime <= .55) {
                fadingOut = true;
                fadeTo(0);
            }
        });

        video.addEventListener('ended', () => {
            video.style.opacity = '0';
            window.setTimeout(() => {
                video.currentTime = 0;
                fadingOut = false;
                video.play().then(() => fadeTo(1)).catch(() => {});
            }, 100);
        });
    }

    const revealItems = document.querySelectorAll('.reveal');
    if (!revealItems.length) return;

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches || !('IntersectionObserver' in window)) {
        revealItems.forEach(item => item.classList.add('is-visible'));
        return;
    }

    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) return;
            entry.target.classList.add('is-visible');
            observer.unobserve(entry.target);
        });
    }, { threshold: .12, rootMargin: '0px 0px -60px' });

    revealItems.forEach(item => observer.observe(item));
})();
