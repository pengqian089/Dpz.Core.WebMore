import { Gallery } from '../js/modules/gallery.js';

export class MumbleObserver {
    constructor() {
        this.observer = null;
        this.dotNetRef = null;
        this.stateMap = new Map();
        this.galleries = new Map();
        this.mutationObserver = null;
        this._clickHandler = (e) => this.handleGalleryClick(e);
        this._imageLoadHandler = (e) => this.markImageLoaded(e.target);
    }

    init(dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.observer = new ResizeObserver(entries => {
            // Skip if DOM processing is paused (e.g., dialog is open)
            if (window.appDOMManager && window.appDOMManager.paused) {
                return;
            }

            for (const entry of entries) {
                const target = entry.target;
                const id = target.getAttribute('data-mumble-id');
                if (!id) continue;

                const height = target.scrollHeight;
                const needExpand = height > 300;

                const lastState = this.stateMap.get(id);
                // Trigger if state changed or if it's the first time (undefined)
                if (lastState !== needExpand) {
                    this.stateMap.set(id, needExpand);
                    try {
                        this.dotNetRef.invokeMethodAsync('UpdateExpandState', id, needExpand);
                    } catch (e) {
                        // Silently ignore if component disposed
                    }
                }
            }
        });

        // 图片点击与加载态（包含详情弹窗等动态插入的网格）
        document.addEventListener('click', this._clickHandler);
        document.addEventListener('load', this._imageLoadHandler, true);
        document.addEventListener('error', this._imageLoadHandler, true);

        this.mutationObserver = new MutationObserver(() => this.markLoadedImages());
        this.mutationObserver.observe(document.body, { childList: true, subtree: true });
        this.markLoadedImages();
    }

    observe(element, id) {
        if (!this.observer || !element) return;
        
        element.setAttribute('data-mumble-id', id);
        this.observer.observe(element);
    }

    /**
     * 点击图片或 +N 时按卡片打开 PhotoSwipe
     */
    handleGalleryClick(event) {
        const target = event.target;
        if (!(target instanceof Element)) return;

        const more = target.closest('[data-gallery-more]');
        const image = target.closest('img.mumble-gallery__image');
        if (!more && !image) return;

        const root = (more ?? image).closest('.mumble-gallery');
        if (!root) return;

        const images = Array.from(root.querySelectorAll('img.mumble-gallery__image'));
        if (images.length === 0) return;

        let gallery = this.galleries.get(root);
        if (!gallery) {
            gallery = new Gallery(images, {
                galleryId: root.getAttribute('data-gallery-id') || 'mumble'
            });
            this.galleries.set(root, gallery);
        }

        if (more) {
            event.preventDefault();
            const hiddenIndex = images.findIndex(img =>
                img.closest('.mumble-gallery__item')?.classList.contains('is-hidden')
            );
            gallery.open(hiddenIndex >= 0 ? hiddenIndex : images.length - 1);
            return;
        }

        gallery.open(images.indexOf(image));
    }

    markImageLoaded(element) {
        if (!(element instanceof HTMLImageElement)) return;
        if (!element.classList.contains('mumble-gallery__image')) return;

        element.closest('.mumble-gallery__item')?.classList.add('is-loaded');
    }

    markLoadedImages() {
        document.querySelectorAll('.mumble-gallery__image').forEach(img => {
            if (img.complete) {
                img.closest('.mumble-gallery__item')?.classList.add('is-loaded');
            }
        });
    }

    dispose() {
        document.removeEventListener('click', this._clickHandler);
        document.removeEventListener('load', this._imageLoadHandler, true);
        document.removeEventListener('error', this._imageLoadHandler, true);

        if (this.mutationObserver) {
            this.mutationObserver.disconnect();
            this.mutationObserver = null;
        }

        this.galleries.forEach(gallery => gallery.cleanup());
        this.galleries.clear();

        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }
        this.dotNetRef = null;
        this.stateMap.clear();
    }
}

export function createObserver() {
    return new MumbleObserver();
}
