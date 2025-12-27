import PhotoSwipeLightbox from 'https://dpangzi.com/library/photoswipe/photoswipe.esm.min.js';
import PhotoSwipe from 'https://dpangzi.com/library/photoswipe/photoswipe.esm.min.js';

export class AlbumsBlazor {
    constructor(dotNetHelper, containerSelector) {
        this.dotNetHelper = dotNetHelper;
        this.containerSelector = containerSelector;
        this.items = [];
        this.lightbox = null;
        this.observer = null;
        this._isClosing = false;
        this._isClosedByNavigation = false;
        
        this.onHashChange = this.onHashChange.bind(this);
    }

    init(initialItems) {
        this.items = initialItems.map(item => this.mapItem(item));
        this.initObserver();
        
        window.addEventListener('hashchange', this.onHashChange);
        // Handle initial hash
        setTimeout(() => this.onHashChange(), 100);
    }

    mapItem(item) {
        return {
            id: item.id || item.Id,
            src: item.accessUrl || item.AccessUrl,
            w: item.width || item.Width,
            h: item.height || item.Height,
            alt: item.description || item.Description
        };
    }

    appendItems(newItems) {
        const mapped = newItems.map(item => this.mapItem(item));
        this.items.push(...mapped);
        
        // Re-check hash in case the requested ID was just loaded
        const id = this.parseHash();
        if (id && !this.lightbox) {
            const index = this.items.findIndex(x => x.id == id);
            if (index >= 0) {
                 this.openGallery(index);
            }
        }
    }

    initObserver() {
        const options = {
            root: null,
            rootMargin: '200px',
            threshold: 0.1
        };

        this.observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    this.dotNetHelper.invokeMethodAsync('LoadMore');
                }
            });
        }, options);

        // We will observe the loading spinner
        const loadingSpinner = document.querySelector(`${this.containerSelector} .albums__loading`);
        if (loadingSpinner) {
            this.observer.observe(loadingSpinner);
        }
    }

    openGallery(index) {
        if (this.lightbox) {
            this.lightbox.pswp.goTo(index);
            return;
        }

        this.updateElements();

        this._isClosing = false;
        const lightbox = new PhotoSwipeLightbox({
            dataSource: this.items,
            pswpModule: PhotoSwipe,
            index: index,
            bgOpacity: 0.9,
            showHideAnimationType: 'zoom'
        });

        this.lightbox = lightbox;

        lightbox.on('change', () => {
             const pswp = lightbox.pswp;
             if (pswp) {
                 const currItem = pswp.currSlide.data;
                 if (currItem && currItem.id) {
                     // Check if hash already matches to avoid redundant history entries
                     const currentHashId = this.parseHash();
                     if (currentHashId !== currItem.id) {
                         const newHash = `pid=${currItem.id}`;
                         // Using pushState here to allow back button navigation between images if desired,
                         // but typically for gallery slides replaceState is often preferred to not clutter history.
                         // However, to match requested behavior "/#pid=id", we just update hash.
                         // To avoid full page reload or weirdness, use history API.
                         
                         // If we want the URL to be /albums/#pid=xxx, we can use:
                         const url = new URL(window.location);
                         url.hash = newHash;
                         history.replaceState(null, '', url.toString());
                     }
                 }
             }
        });

        lightbox.on('close', () => {
            this._isClosing = true;
            // When closing, remove the hash
             if (!this._isClosedByNavigation && this.parseHash() !== null) {
                 // Go back if we added history state, or just replace state to remove hash
                 // If we used replaceState above, we should just remove the hash here
                 const url = new URL(window.location);
                 url.hash = '';
                 // We use replaceState to clear the hash without adding a new history entry
                 // OR if the user expects back button behavior, we might need history.back() 
                 // if the hash was added via pushState.
                 // Given the previous code used history.back(), let's stick to a cleaner approach:
                 // remove hash silently.
                 history.replaceState(null, '', window.location.pathname + window.location.search);
             }
            this.lightbox = null;
            this._isClosedByNavigation = false;
        });

        // Set initial hash
        const item = this.items[index];
        if (item && item.id) {
             const newHash = `pid=${item.id}`;
             const currentHashId = this.parseHash();
             if (currentHashId !== item.id) {
                 const url = new URL(window.location);
                 url.hash = newHash;
                 history.pushState(null, '', url.toString());
             }
        }

        lightbox.init();
    }

    updateElements() {
        const images = document.querySelectorAll(`${this.containerSelector} .albums__image`);
        images.forEach((img, idx) => {
            if (this.items[idx]) {
                this.items[idx].element = img;
            }
        });
    }

    parseHash() {
        const hash = window.location.hash;
        // Match #pid=... or #/pid=... or just pid=... inside hash
        // The user mentioned "/#pid=id", which might technically be a fragment like "#pid=id"
        // But some routers use "#/route". 
        // Let's look for "pid=" anywhere in the hash
        const match = hash.match(/pid=([^&]+)/);
        if (match) {
            return match[1];
        }
        return null;
    }

    onHashChange() {
        const id = this.parseHash();
        if (id !== null) {
            const index = this.items.findIndex(x => x.id == id);
            if (index >= 0) {
                this.openGallery(index);
            }
        } else {
            if (this.lightbox && !this._isClosing) {
                this._isClosedByNavigation = true;
                if (this.lightbox.pswp) {
                    this.lightbox.pswp.close();
                }
            }
        }
    }

    dispose() {
        if (this.observer) this.observer.disconnect();
        window.removeEventListener('hashchange', this.onHashChange);
        if (this.lightbox) {
            this.lightbox.destroy();
        }
    }
    
    downloadImage(url) {
        const link = document.createElement('a');
        link.href = url;
        link.download = '';
        link.target = '_blank';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
    
    async shareImage(url) {
        if (navigator.share) {
            try {
                await navigator.share({
                    title: '分享图片',
                    url: url
                });
            } catch (e) {
                console.error(e);
            }
        } else {
             navigator.clipboard.writeText(url).then(() => {
                alert('图片链接已复制到剪贴板');
            }).catch(() => {
                prompt('复制链接:', url);
            });
        }
    }
}

export function create(dotNetHelper, containerSelector) {
    return new AlbumsBlazor(dotNetHelper, containerSelector);
}
