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
    
    async downloadImage(url) {
        try {
            // 使用 fetch 获取图片 blob，然后触发下载
            const response = await fetch(url);
            const blob = await response.blob();
            
            // 从 URL 中提取文件名
            const urlParts = url.split('/');
            let filename = urlParts[urlParts.length - 1].split('?')[0].split('!')[0];
            if (!filename || filename.length < 3) {
                filename = `image_${Date.now()}.jpg`;
            }
            
            // 创建 blob URL 并下载
            const blobUrl = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = blobUrl;
            link.download = filename;
            link.style.display = 'none';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            
            // 清理 blob URL
            setTimeout(() => URL.revokeObjectURL(blobUrl), 100);
        } catch (e) {
            console.error('下载失败:', e);
            // 降级方案：在新窗口打开图片
            window.open(url, '_blank');
        }
    }
    
    async shareImage(url) {
        // 优先使用 Web Share API（移动端支持较好）
        if (navigator.share) {
            try {
                await navigator.share({
                    title: '分享图片',
                    text: '来看看这张图片',
                    url: url
                });
                return null; // 分享成功，不需要 Toast
            } catch (e) {
                // 用户取消分享或不支持，降级到复制链接
                if (e.name === 'AbortError') {
                    return null; // 用户取消，不显示 Toast
                }
                console.warn('Web Share API 失败:', e);
            }
        }
        
        // 降级方案：复制链接到剪贴板
        try {
            await navigator.clipboard.writeText(url);
            return '链接已复制到剪贴板';
        } catch (e) {
            // 最终降级：使用 prompt
            prompt('复制图片链接:', url);
            return null;
        }
    }
}

export function create(dotNetHelper, containerSelector) {
    return new AlbumsBlazor(dotNetHelper, containerSelector);
}
