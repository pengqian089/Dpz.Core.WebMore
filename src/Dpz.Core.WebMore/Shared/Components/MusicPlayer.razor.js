export class AudioPlayer {
    constructor(dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        this.audio = new Audio();
        this.setupEvents();
        this.initTouchEvents('mp-mini');
    }

    setupEvents() {
        this.audio.addEventListener('timeupdate', () => {
            this.dotNetHelper.invokeMethodAsync('OnTimeUpdate', this.audio.currentTime);
        });

        this.audio.addEventListener('ended', () => {
            this.dotNetHelper.invokeMethodAsync('OnEnded');
        });

        this.audio.addEventListener('durationchange', () => {
            this.dotNetHelper.invokeMethodAsync('OnDurationChange', this.audio.duration);
        });

        this.audio.addEventListener('error', (e) => {
            this.dotNetHelper.invokeMethodAsync('OnError', this.audio.error.message);
        });
        
        this.audio.addEventListener('play', () => {
             this.dotNetHelper.invokeMethodAsync('OnPlayStateChange', true);
        });

        this.audio.addEventListener('pause', () => {
             this.dotNetHelper.invokeMethodAsync('OnPlayStateChange', false);
        });

        if ('mediaSession' in navigator) {
            navigator.mediaSession.setActionHandler('play', () => this.play());
            navigator.mediaSession.setActionHandler('pause', () => this.pause());
            navigator.mediaSession.setActionHandler('previoustrack', () => this.dotNetHelper.invokeMethodAsync('OnPrev'));
            navigator.mediaSession.setActionHandler('nexttrack', () => this.dotNetHelper.invokeMethodAsync('OnNext'));
        }
    }

    setSrc(src) {
        this.audio.src = src;
    }

    play() {
        return this.audio.play().catch(e => console.warn("Playback prevented:", e));
    }

    pause() {
        this.audio.pause();
    }

    setVolume(volume) {
        this.audio.volume = volume;
    }

    setCurrentTime(time) {
        this.audio.currentTime = time;
    }

    getDuration() {
        return this.audio.duration;
    }
    
    updateMediaSession(title, artist, cover) {
        if ('mediaSession' in navigator) {
            navigator.mediaSession.metadata = new MediaMetadata({
                title: title,
                artist: artist,
                artwork: [
                    { src: cover, sizes: '512x512', type: 'image/jpeg' }
                ]
            });
        }
    }

    scrollToItem(elementId, block = 'center') {
        const el = document.getElementById(elementId);
        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: block });
        }
    }

    scrollToBgLyric(index) {
         const container = document.getElementById('mp-bg-lyrics-inner');
         if (!container) return;
         const lines = container.children;
         if (lines[index]) {
             const offset = lines[index].offsetTop;
             container.style.transform = `translateY(-${offset}px)`;
         }
    }

    initTouchEvents(elementId) {
        const element = document.getElementById(elementId);
        if (!element) return;

        let timer;
        let isLongPress = false;

        const start = (e) => {
            isLongPress = false;
            timer = setTimeout(() => {
                isLongPress = true;
                this.dotNetHelper.invokeMethodAsync('OpenPanel');
                if (navigator.vibrate) navigator.vibrate(50);
            }, 600);
        };

        const end = (e) => {
            clearTimeout(timer);
            if (isLongPress) {
                if (e.cancelable) {
                    e.preventDefault();
                }
            }
        };

        const move = () => {
            clearTimeout(timer);
        };

        element.addEventListener('touchstart', start, { passive: true });
        element.addEventListener('touchend', end);
        element.addEventListener('touchmove', move, { passive: true });
        element.addEventListener('touchcancel', move);
    }
}

export function initAudioPlayer(dotNetHelper) {
    return new AudioPlayer(dotNetHelper);
}

