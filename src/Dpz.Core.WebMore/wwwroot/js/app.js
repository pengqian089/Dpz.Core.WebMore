import {CodeArea} from "./modules/code-area.js";
import {BackToTop} from "./modules/back-to-top.js";
import {Gallery} from "./modules/gallery.js";
import {DOMChangeManager} from "./modules/document-change-manager.js";


class App {
    constructor() {
        this.domManager = new DOMChangeManager();
        // 暴露到 window，让 Gallery 可以访问
        window.appDOMManager = this.domManager;
        this.init();
    }

    init() {
        // 初始化 LazyLoad
        this.lazyLoadInstance = new LazyLoad({
            elements_selector: ".lazy",
            class_loading: "lazy-loading",
            class_loaded: "lazy-loaded",
            class_error: "lazy-error",
        });

        // 注册各个功能模块的处理器
        this.registerHandlers();

        // 开始监听 DOM 变化
        this.domManager.start();
    }

    /**
     * 注册所有功能模块
     */
    registerHandlers() {
        // 1. LazyLoad 处理器
        this.domManager.register({
            name: 'LazyLoad',
            selectors: ['.lazy'],
            onInit: () => {
                // 首次加载时更新
                this.lazyLoadInstance.update();
            },
            onUpdate: () => {
                this.lazyLoadInstance.update();
            }
        });

        // 2. 代码高亮处理器
        this.domManager.register({
            name: 'CodeArea',
            selectors: ['pre code'],
            onInit: () => {
                new CodeArea();
            },
            onUpdate: () => {
                new CodeArea();
            }
        });

        // 3. 图片画廊处理器
        this.domManager.register({
            name: 'Gallery',
            selectors: ['.article-card__cover img', '.markdown-body img'],
            onInit: () => {
                this.initGallery();
            },
            onUpdate: () => {
                // 当有属性变化时（如 lazy-loaded 类添加），只更新光标状态
                // 避免频繁重新初始化整个 Gallery
                this.updateGallery();
            }
        });

        // 4. 返回顶部处理器
        this.domManager.register({
            name: 'BackToTop',
            selectors: ['#back-to-top'],
            onInit: () => {
                this.initBackToTop();
            },
            onUpdate: () => {
                // 如果之前没有成功初始化，再试一次
                if (!this.backToTopInstance) {
                    this.initBackToTop();
                }
            }
        });
    }

    initBackToTop() {
        const element = document.getElementById('back-to-top');
        if (element && !this.backToTopInstance) {
            this.backToTopInstance = new BackToTop();
        }
    }
    
    initGallery() {
        const articleImages = document.querySelectorAll('.article-card__cover img, .markdown-body img');
        
        if (articleImages.length > 0) {
            // 清理旧实例
            if (this.galleryInstance) {
                this.galleryInstance.cleanup();
            }
            
            // 创建新实例
            this.galleryInstance = new Gallery(articleImages);
        }
    }
    
    updateGallery() {
        // 如果还没有 Gallery 实例，先初始化
        if (!this.galleryInstance) {
            this.initGallery();
            return;
        }
        
        // 检查是否有新图片
        const currentImages = document.querySelectorAll('.article-card__cover img, .markdown-body img');
        const currentCount = currentImages.length;
        const previousCount = this.galleryInstance.images.length;
        
        if (currentCount !== previousCount) {
            // 有新图片添加或删除，需要重新初始化
            this.initGallery();
        } else {
            // 只是属性变化（如 lazy-loaded 类添加），只更新光标
            this.galleryInstance.updateAllCursors();
        }
    }
}

// 确保在 DOM 加载完成后执行
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => new App());
} else {
    new App();
}

// Dialog Interop
window.dialogInterop = {
    disableBodyScroll: function () {
        document.body.style.overflow = 'hidden';
    },
    enableBodyScroll: function () {
        document.body.style.overflow = '';
    }
};