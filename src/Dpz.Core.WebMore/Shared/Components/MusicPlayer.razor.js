export class AudioPlayer {
    constructor(dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        this.audio = new Audio();
        this.storageKey = 'dpz_music_player_state';
        this.saveInterval = null;
        this.setupEvents();
    }
    
    
    // 状态保存到 localStorage
    saveState(state) {
        try {
            localStorage.setItem(this.storageKey, JSON.stringify(state));
        } catch (e) {
            console.warn('Failed to save music player state:', e);
        }
    }
    
    loadState() {
        try {
            const data = localStorage.getItem(this.storageKey);
            return data ? JSON.parse(data) : null;
        } catch (e) {
            console.warn('Failed to load music player state:', e);
            return null;
        }
    }
    
    // 开始定时保存播放进度
    startProgressSave(trackId) {
        this.stopProgressSave();
        this.currentTrackId = trackId;
        this.saveInterval = setInterval(() => {
            if (!this.audio.paused && this.currentTrackId) {
                this.dotNetHelper.invokeMethodAsync('SavePlayProgress', this.currentTrackId, this.audio.currentTime);
            }
        }, 5000);
    }
    
    stopProgressSave() {
        if (this.saveInterval) {
            clearInterval(this.saveInterval);
            this.saveInterval = null;
        }
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
             // 开始播放时启动进度保存
             if (this.currentTrackId) {
                 this.startProgressSave(this.currentTrackId);
             }
        });

        this.audio.addEventListener('pause', () => {
             this.dotNetHelper.invokeMethodAsync('OnPlayStateChange', false);
             // 暂停时立即保存一次进度
             if (this.currentTrackId) {
                 this.dotNetHelper.invokeMethodAsync('SavePlayProgress', this.currentTrackId, this.audio.currentTime);
             }
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

}

export function initAudioPlayer(dotNetHelper) {
    return new AudioPlayer(dotNetHelper);
}

export class MusicPlayerUi {
    constructor(dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        this._panelOpen = false;
        this._onHashChange = () => {
            const hash = window.location.hash;
            if (hash === '#music-player') {
                this.dotNetHelper.invokeMethodAsync('OpenPanel');
            } else if (this._panelOpen && hash !== '#music-player') {
                this.dotNetHelper.invokeMethodAsync('ClosePanel');
            }
        };

        window.addEventListener('hashchange', this._onHashChange);

        if (window.location.hash === '#music-player') {
            setTimeout(() => {
                this.dotNetHelper.invokeMethodAsync('OpenPanel');
            }, 100);
        }

        this.initTouchEvents('mp-mini');
    }

    setPanelOpen(isOpen) {
        this._panelOpen = isOpen;

        const isMobile = window.innerWidth <= 768;

        if (isOpen) {
            this._originalUrl = window.location.pathname + window.location.search;
            if (window.location.hash !== '#music-player') {
                history.pushState(null, '', this._originalUrl + '#music-player');
            }
            if (isMobile) {
                document.body.style.overflow = 'hidden';
            }
        } else {
            if (window.location.hash === '#music-player') {
                const restoreUrl = this._originalUrl || (window.location.pathname + window.location.search);
                history.pushState(null, '', restoreUrl);
            }
            document.body.style.overflow = '';
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

    dispose() {
        window.removeEventListener('hashchange', this._onHashChange);
    }
}

export function initMusicPlayerUi(dotNetHelper) {
    return new MusicPlayerUi(dotNetHelper);
}

