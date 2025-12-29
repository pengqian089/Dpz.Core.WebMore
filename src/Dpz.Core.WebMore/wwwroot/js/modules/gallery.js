import PhotoSwipeLightbox from 'https://dpangzi.com/library/photoswipe/photoswipe.esm.min.js';
import PhotoSwipe from 'https://dpangzi.com/library/photoswipe/photoswipe.esm.min.js';

export class Gallery {
    /**
     * @param {NodeListOf<Element>|HTMLImageElement[]} images 
     * @param {Object} [options]
     * @param {boolean} [options.preloadAll=false]
     * @param {number} [options.preloadNeighbors=1]
     * @param {string|number} [options.galleryId=1]
     */
    constructor(images, options = {}) {
        this.images = Array.from(images);
        this.items = [];
        this.options = Object.assign({
            preloadAll: false,
            preloadNeighbors: 1,
            galleryId: 1
        }, options);
        
        this.lightbox = null;
        this._isClosedByNavigation = false;
        this._isClosing = false;
        this._clickHandlers = new Map(); // 存储事件处理器，用于清理
        this._loadingIndicator = null; // 加载提示元素

        this.init();
        this.checkHash();
        window.addEventListener('hashchange', () => this.handleHashChange());
    }

    init() {
        // 清理旧的事件监听器（如果存在）
        this.cleanup();
        
        this.images.forEach((img, index) => {
            this.bindImage(img, index);
        });
    }
    
    /**
     * 绑定单张图片
     * @param {HTMLImageElement} img 
     * @param {number} index 
     */
    bindImage(img, index) {
        // Prevent duplicate binding
        if (img.dataset.pswpBound) {
            // 如果已经绑定过，只更新光标
            this.updateImageCursor(img);
            return;
        }
        img.dataset.pswpBound = "true";

        // Determine image sources
        // 获取原图 URL（优先使用 data-origin）
        const originalSrc = this.getImageSrc(img);
        
        // Item configuration
        const item = {
            src: originalSrc,
            w: 0,
            h: 0,
            element: img,
            index: index,
            loading: false
        };
        
        // 关键修复：只有在没有 data-origin 的情况下才使用当前图片尺寸
        // 如果有 data-origin，说明当前显示的是缩略图，尺寸不准确
        // 同时也必须排除占位符图片（如 loaders/oval.svg）
        const isPlaceholder = img.src && img.src.includes('loaders/oval.svg');
        
        if (img.complete && img.naturalWidth > 0 && !img.dataset.origin && !isPlaceholder) {
            // 当前图片就是原图，可以直接使用尺寸
            item.w = img.naturalWidth;
            item.h = img.naturalHeight;
            console.log(`[Gallery] 图片 ${index} 初始化时获取尺寸:`, item.w, 'x', item.h);
        } else if (img.dataset.origin) {
            console.log(`[Gallery] 图片 ${index} 有 data-origin，需要加载原图尺寸`);
        } else if (isPlaceholder) {
            console.log(`[Gallery] 图片 ${index} 是占位符，需要加载原图尺寸`);
        }
        
        this.items.push(item);

        // Bind click event and store handler for cleanup
        const clickHandler = (e) => this.handleClick(e, index);
        this._clickHandlers.set(img, clickHandler);
        img.addEventListener('click', clickHandler);
        
        // 根据图片加载状态设置光标样式
        this.updateImageCursor(img);

        if (this.options.preloadAll) {
            this.preloadImage(item);
        }
    }
    
    /**
     * 更新所有图片的光标状态（用于 LazyLoad 完成后）
     */
    updateAllCursors() {
        let loadedCount = 0;
        let loadingCount = 0;
        let waitingCount = 0;
        
        this.images.forEach(img => {
            if (img.classList.contains('lazy-loaded')) loadedCount++;
            else if (img.classList.contains('lazy-loading')) loadingCount++;
            else if (img.classList.contains('lazy')) waitingCount++;
            
            this.updateImageCursor(img);
        });
        
        console.log(`[Gallery] 光标更新完成 - 已加载: ${loadedCount}, 加载中: ${loadingCount}, 等待: ${waitingCount}`);
    }
    
    /**
     * 清理事件监听器和相关资源
     */
    cleanup() {
        // 移除所有存储的事件监听器
        this._clickHandlers.forEach((handler, img) => {
            img.removeEventListener('click', handler);
            delete img.dataset.pswpBound;
        });
        this._clickHandlers.clear();
        
        // 关闭可能打开的 lightbox
        if (this.lightbox && this.lightbox.pswp) {
            this.lightbox.close();
        }
        
        // 隐藏加载提示
        this.hideLoadingIndicator();
        
        // 重置状态
        this.items = [];
        this.lightbox = null;
        this._isClosedByNavigation = false;
        this._isClosing = false;
    }
    

    /**
     * Get the highest priority image source (初始化时使用)
     * @param {HTMLImageElement} img 
     * @returns {string}
     */
    getImageSrc(img) {
        if (img.dataset.origin && img.dataset.origin.trim() !== '') {
            return img.dataset.origin;
        }
        if (img.dataset.src && img.dataset.src.trim() !== '') {
            return img.dataset.src;
        }
        return img.src;
    }
    
    /**
     * Get actual loaded image source (LazyLoad 完成后使用)
     * @param {HTMLImageElement} img 
     * @returns {string}
     */
    getActualImageSrc(img) {
        // 优先使用 data-origin（如果存在，这是原图地址）
        if (img.dataset.origin && img.dataset.origin.trim() !== '') {
            return img.dataset.origin;
        }
        
        // 使用实际加载的图片 URL（img.src）
        // 但要排除占位符
        const currentSrc = img.src;
        if (currentSrc && !currentSrc.includes('loaders/oval.svg')) {
            return currentSrc;
        }
        
        // 后备：使用 data-src
        if (img.dataset.src && img.dataset.src.trim() !== '' && !img.dataset.src.includes('loaders/oval.svg')) {
            return img.dataset.src;
        }
        
        return currentSrc;
    }
    
    /**
     * 根据图片加载状态更新光标样式
     * @param {HTMLImageElement} img 
     */
    updateImageCursor(img) {
        const DEBUG = false; // 设置为 true 启用详细调试日志
        
        const classes = Array.from(img.classList);
        const complete = img.complete;
        const naturalWidth = img.naturalWidth;
        
        let cursor = 'wait';
        let reason = '默认等待';
        
        // 正在加载中
        if (img.classList.contains('lazy-loading')) {
            cursor = 'wait';
            reason = '正在加载';
        }
        // 已加载完成（优先检查，因为 lazy 类不会被移除）
        else if (img.classList.contains('lazy-loaded')) {
            cursor = 'zoom-in';
            reason = 'LazyLoad 已完成';
        }
        // 加载失败
        else if (img.classList.contains('lazy-error')) {
            cursor = 'not-allowed';
            reason = 'LazyLoad 失败';
        }
        // 还未开始加载（未进入视口）
        else if (img.classList.contains('lazy')) {
            cursor = 'wait';
            reason = '等待 LazyLoad';
        }
        // 非 LazyLoad 图片，检查实际加载状态
        else if (complete && naturalWidth === 0) {
            cursor = 'not-allowed';
            reason = '图片加载失败';
        }
        else if (complete && naturalWidth > 0) {
            cursor = 'zoom-in';
            reason = '图片已加载';
        }
        
        if (DEBUG) {
            console.log('[Cursor]', {
                src: img.src.substring(img.src.lastIndexOf('/') + 1),
                classes: classes,
                complete: complete,
                naturalWidth: naturalWidth,
                cursor: cursor,
                reason: reason
            });
        }
        
        img.style.cursor = cursor;
    }

    /**
     * 显示加载提示
     */
    showLoadingIndicator() {
        if (this._loadingIndicator) return; // 已经在显示了
        
        const indicator = document.createElement('div');
        indicator.className = 'gallery-loading-indicator';
        indicator.innerHTML = `
            <div class="gallery-loading-spinner"></div>
            <div class="gallery-loading-text">正在加载图片...</div>
        `;
        
        // 添加样式
        indicator.style.cssText = `
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background: rgba(0, 0, 0, 0.8);
            color: white;
            padding: 20px 30px;
            border-radius: 8px;
            z-index: 9999;
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 10px;
            font-size: 14px;
        `;
        
        const spinner = indicator.querySelector('.gallery-loading-spinner');
        spinner.style.cssText = `
            width: 30px;
            height: 30px;
            border: 3px solid rgba(255, 255, 255, 0.3);
            border-top-color: white;
            border-radius: 50%;
            animation: gallery-spin 0.8s linear infinite;
        `;
        
        // 添加动画
        if (!document.querySelector('#gallery-loading-style')) {
            const style = document.createElement('style');
            style.id = 'gallery-loading-style';
            style.textContent = `
                @keyframes gallery-spin {
                    to { transform: rotate(360deg); }
                }
            `;
            document.head.appendChild(style);
        }
        
        document.body.appendChild(indicator);
        this._loadingIndicator = indicator;
    }

    /**
     * 隐藏加载提示
     */
    hideLoadingIndicator() {
        if (this._loadingIndicator) {
            this._loadingIndicator.remove();
            this._loadingIndicator = null;
        }
    }

    preloadImage(item) {
        if (item.w > 0 && item.h > 0) return;
        if (item.loading) return;

        item.loading = true;
        const preloadImg = new Image();
        preloadImg.src = item.src;
        preloadImg.onload = () => {
            item.w = preloadImg.naturalWidth;
            item.h = preloadImg.naturalHeight;
            item.loading = false;
        };
        preloadImg.onerror = () => {
            item.loading = false;
        };
    }

    preloadNeighbors(currentIndex) {
        const count = this.options.preloadNeighbors;
        
        // Always preload current
        this.preloadImage(this.items[currentIndex]);

        // Preload neighbors
        for (let i = 1; i <= count; i++) {
            const nextIndex = currentIndex + i;
            const prevIndex = currentIndex - i;

            if (nextIndex < this.items.length) {
                this.preloadImage(this.items[nextIndex]);
            }
            if (prevIndex >= 0) {
                this.preloadImage(this.items[prevIndex]);
            }
        }
    }

    handleClick(e, index) {
        e.preventDefault();
        
        console.time('[Gallery] 从点击到首次渲染');

        const item = this.items[index];
        const imgElement = item.element;
        
        // 检查图片是否正在延迟加载中
        if (imgElement.classList.contains('lazy-loading')) {
            console.log('[Gallery] 图片正在加载中，请稍候...');
            return;
        }
        
        // 如果有 lazy 类但没有 lazy-loaded 类，说明还未开始加载（未进入视口）
        if (imgElement.classList.contains('lazy') && 
            !imgElement.classList.contains('lazy-loaded') && 
            !imgElement.classList.contains('lazy-error')) {
            console.log('[Gallery] 图片还未加载，请等待图片进入可视区域');
            return;
        }
        
        // 检查图片是否加载失败
        if (imgElement.complete && imgElement.naturalWidth === 0) {
            console.warn('[Gallery] 图片加载失败');
            return;
        }

        // 如果 item 已有尺寸信息，直接打开
        if (item.w > 0 && item.h > 0) {
            console.log('[Gallery] 使用缓存的尺寸信息:', item.w, 'x', item.h);
            this.open(index);
            return;
        }

        // 关键修复：如果有 data-origin，说明当前显示的是缩略图
        // 不能使用当前图片元素的尺寸，必须加载原图获取正确尺寸
        if (imgElement.dataset.origin) {
            console.log('[Gallery] 检测到 data-origin，需要加载原图尺寸');
            // 跳过使用当前元素尺寸的逻辑，直接进入加载原图
        } else if (imgElement.complete && imgElement.naturalWidth > 0 && !imgElement.src.includes('loaders/oval.svg')) {
            // 没有 data-origin 且不是占位符，当前图片就是原图，可以直接使用尺寸
            console.log('[Gallery] 使用图片元素的尺寸信息:', imgElement.naturalWidth, 'x', imgElement.naturalHeight);
            item.w = imgElement.naturalWidth;
            item.h = imgElement.naturalHeight;
            
            // 确保 item.src 是正确的
            const actualSrc = this.getActualImageSrc(imgElement);
            if (actualSrc !== item.src) {
                console.log('[Gallery] 更新图片 URL:', actualSrc);
                item.src = actualSrc;
            }
            
            this.open(index);
            return;
        }

        // 需要加载原图获取尺寸
        // 确保使用正确的原图 URL
        if (item.src.includes('loaders/oval.svg')) {
            const actualSrc = this.getImageSrc(imgElement);
            console.log('[Gallery] 修正图片 URL:', item.src, '->', actualSrc);
            item.src = actualSrc;
        }
        
        console.log('[Gallery] 加载原图获取尺寸:', item.src);
        this.showLoadingIndicator();
        
        const img = new Image();
        img.src = item.src;
        img.onload = () => {
            item.w = img.naturalWidth;
            item.h = img.naturalHeight;
            this.hideLoadingIndicator();
            console.log('[Gallery] 尺寸加载完成:', item.w, 'x', item.h);
            this.open(index);
        };
        img.onerror = () => {
            this.hideLoadingIndicator();
            console.error('[Gallery] 原图加载失败:', item.src);
            // 即使加载失败，也尝试打开（PhotoSwipe 会显示错误信息）
            this.open(index);
        };
    }

    open(index, fromHash = false) {
        // If already open, just update index
        if (this.lightbox && this.lightbox.pswp) {
            this.lightbox.pswp.goTo(index);
            return;
        }

        this._isClosing = false;
        
        // 暂停 DOM 监听，避免 PhotoSwipe 打开时的 DOM 变化触发 Gallery 更新
        if (window.appDOMManager) {
            window.appDOMManager.pause();
        }
        
        // 调试信息：打印当前要打开的图片信息
        const currentItem = this.items[index];
        console.log('[Gallery] 打开画廊 - 当前图片:', {
            index: index,
            src: currentItem.src,
            w: currentItem.w,
            h: currentItem.h
        });
        
        // 调试信息：检查所有图片的 URL 是否正确
        console.log('[Gallery] 所有图片信息:', this.items.map((item, i) => ({
            index: i,
            src: item.src.includes('loaders') ? '❌占位符' : '✅正常',
            url: item.src,
            w: item.w,
            h: item.h
        })));

        console.time('[Gallery] PhotoSwipe 初始化耗时');
        const lightbox = new PhotoSwipeLightbox({
            dataSource: this.items,
            pswpModule: PhotoSwipe,
            index: index,
            bgOpacity: 0.9,
            showHideAnimationType: 'zoom',
            errorMsg: '<div class="pswp__error-msg">图片加载失败</div>'
        });

        this.lightbox = lightbox;

        // Preload based on config
        if (!this.options.preloadAll) {
            this.preloadNeighbors(index);
            
            lightbox.on('change', () => {
                const newIndex = lightbox.currIndex;
                this.preloadNeighbors(newIndex);
                this.updateHash(newIndex);
            });
        } else {
             lightbox.on('change', () => {
                this.updateHash(lightbox.currIndex);
            });
        }

        // Handle dimensions if not yet preloaded
        lightbox.on('contentLoad', (e) => {
            const { content } = e;
            
            // If dimensions are already known (from preload), let it proceed
            if (content.data.w > 0 && content.data.h > 0) {
                return;
            }

            // Do NOT prevent default here. 
            // We want PhotoSwipe to proceed with creating the slide elements,
            // even if dimensions are temporarily 0.
            // e.preventDefault(); 
            
            const img = new Image();
            img.src = content.data.src;
            
            img.onload = () => {
                content.data.w = img.naturalWidth;
                content.data.h = img.naturalHeight;
                
                // Update item for future use
                const item = lightbox.options.dataSource[content.index];
                if (item) {
                    item.w = img.naturalWidth;
                    item.h = img.naturalHeight;
                    item.loading = false;
                }
            };
            
            img.onerror = () => {
                console.error('Failed to load image:', content.data.src);
            };
        });

        // 当画廊打开后，隐藏加载提示
        lightbox.on('firstUpdate', () => {
            // firstUpdate 在画廊首次渲染后触发
            console.timeEnd('[Gallery] PhotoSwipe 初始化耗时');
            console.timeEnd('[Gallery] 从点击到首次渲染');
            this.hideLoadingIndicator();
        });

        lightbox.on('close', () => {
            this._isClosing = true;
            
            // 清除 hash（如果是当前画廊的 hash）
            if (!this._isClosedByNavigation && window.location.hash.includes(`gid=${this.options.galleryId}`)) {
                console.log('[Gallery] 清除 hash');
                this.clearHash();
            }
            
            this.lightbox = null;
            this._isClosedByNavigation = false;
            
            // 恢复 DOM 监听
            if (window.appDOMManager) {
                // 延迟恢复，确保关闭动画完成
                setTimeout(() => {
                    window.appDOMManager.resume();
                }, 300);
            }
        });

        // If not opened from hash, push the initial state
        if (!fromHash) {
            this.pushHash(index);
        }

        console.log('[Gallery] 开始初始化 PhotoSwipe...');
        lightbox.init();
        console.log('[Gallery] lightbox.init() 调用完成');
    }

    // Hash Helper Methods
    
    parseHash() {
        const hash = window.location.hash.substring(1);
        const params = {};
        if (hash.length < 5) return params;

        const vars = hash.split('&');
        for (let i = 0; i < vars.length; i++) {
            if (!vars[i]) continue;
            const pair = vars[i].split('=');
            if (pair.length < 2) continue;
            params[pair[0]] = pair[1];
        }

        if (params.gid) params.gid = parseInt(params.gid, 10);
        if (params.pid) params.pid = parseInt(params.pid, 10);
        return params;
    }

    checkHash() {
        const params = this.parseHash();
        if (params.gid === this.options.galleryId && params.pid > 0 && params.pid <= this.items.length) {
            // 从 hash 打开时，先确保图片尺寸已加载
            this.openFromHash(params.pid - 1);
        }
    }

    handleHashChange() {
        const params = this.parseHash();
        
        // If hash matches this gallery
        if (params.gid === this.options.galleryId && params.pid > 0 && params.pid <= this.items.length) {
            // 从 hash 打开时，先确保图片尺寸已加载
            this.openFromHash(params.pid - 1);
        } else {
            // Hash doesn't match or is empty
            if (this.lightbox && !this._isClosing) {
                this._isClosedByNavigation = true;
                this.lightbox.close();
            }
        }
    }

    /**
     * 从 hash 打开画廊（确保图片尺寸已加载）
     * @param {number} index 
     */
    openFromHash(index) {
        console.log('[Gallery] 从 hash 打开画廊，index:', index);
        const item = this.items[index];
        const imgElement = item.element;
        
        // 如果尺寸已知，直接打开
        if (item.w > 0 && item.h > 0) {
            this.open(index, true);
            return;
        }

        // 检查图片元素是否已经加载完成（LazyLoad 可能已完成）
        // 但如果有 data-origin，说明当前是缩略图，不能使用其尺寸
        if (imgElement.complete && imgElement.naturalWidth > 0 && !imgElement.dataset.origin && !imgElement.src.includes('loaders/oval.svg')) {
            console.log('[Gallery] 从 hash 打开：使用图片元素的尺寸');
            item.w = imgElement.naturalWidth;
            item.h = imgElement.naturalHeight;
            
            // 更新 item.src 为实际图片 URL
            const actualSrc = this.getActualImageSrc(imgElement);
            if (actualSrc !== item.src) {
                console.log('[Gallery] 从 hash 打开：更新图片 URL:', actualSrc);
                item.src = actualSrc;
            }
            
            this.open(index, true);
            return;
        }

        // 需要加载图片获取尺寸
        console.log('[Gallery] 从 hash 打开：加载图片尺寸');
        this.showLoadingIndicator();
        
        // 确保使用正确的原图 URL
        if (item.src.includes('loaders/oval.svg') || imgElement.dataset.origin) {
            const actualSrc = this.getImageSrc(imgElement);
            console.log('[Gallery] 从 hash 打开：使用原图 URL:', actualSrc);
            item.src = actualSrc;
        }
        
        const img = new Image();
        img.src = item.src;
        
        img.onload = () => {
            item.w = img.naturalWidth;
            item.h = img.naturalHeight;
            this.hideLoadingIndicator();
            console.log('[Gallery] 从 hash 打开：尺寸加载完成');
            this.open(index, true);
        };
        
        img.onerror = () => {
            this.hideLoadingIndicator();
            console.error('[Gallery] 从 hash 加载图片失败:', item.src);
            // 即使加载失败，也尝试打开
            this.open(index, true);
        };
    }

    pushHash(index) {
        const hash = `&gid=${this.options.galleryId}&pid=${index + 1}`;
        // 保留当前的 pathname 和 search，只添加 hash
        const newUrl = `${window.location.pathname}${window.location.search}#${hash}`;
        // 使用 replaceState 而不是 pushState，避免添加到历史记录
        window.history.replaceState(null, '', newUrl);
    }

    updateHash(index) {
        const hash = `&gid=${this.options.galleryId}&pid=${index + 1}`;
        // 保留当前的 pathname 和 search，只更新 hash
        const newUrl = `${window.location.pathname}${window.location.search}#${hash}`;
        window.history.replaceState(null, '', newUrl);
    }
    
    clearHash() {
        // 清除 hash，恢复到原始 URL
        const newUrl = `${window.location.pathname}${window.location.search}`;
        window.history.replaceState(null, '', newUrl);
    }
}
