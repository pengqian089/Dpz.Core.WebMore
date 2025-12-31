import {CodeArea} from "./modules/code-area.js";
import {BackToTop} from "./modules/back-to-top.js";
import {Gallery} from "./modules/gallery.js";
import {DOMChangeManager} from "./modules/document-change-manager.js";


class App {
    constructor() {
        this.domManager = new DOMChangeManager();
        // 暴露到 window，让 Gallery 可以访问
        window.appDOMManager = this.domManager;
        this.lazyLoadInstance = null; // 延迟初始化
        
        // 创建 Promise 等待 Blazor 启动通知
        this.blazorStartedPromise = new Promise((resolve) => {
            this.blazorStartedResolver = resolve;
        });
        
        this.init();
    }

    init() {
        // 注册各个功能模块的处理器
        this.registerHandlers();

        // 开始监听 DOM 变化
        this.domManager.start();
        
        // 等待 Blazor 启动完成后再初始化 LazyLoad
        this.blazorStartedPromise.then(() => {
            this.initLazyLoad();
        });
    }
    
    /**
     * 由 Blazor MainLayout 首次渲染后调用
     * 通知 JS 应用已启动完成
     */
    onBlazorStarted() {
        if (this.blazorStartedResolver) {
            this.blazorStartedResolver();
        }
    }
    
    /**
     * 初始化 LazyLoad
     */
    initLazyLoad() {
        if (this.lazyLoadInstance) {
            // 如果已经存在，更新即可
            this.lazyLoadInstance.update();
            return;
        }
        
        // 创建 LazyLoad 实例
        this.lazyLoadInstance = new LazyLoad({
            elements_selector: ".lazy",
            class_loading: "lazy-loading",
            class_loaded: "lazy-loaded",
            class_error: "lazy-error",
        });
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
                // 首次加载时初始化（Blazor 可能还没准备好）
                this.initLazyLoad();
            },
            onUpdate: () => {
                // DOM 变化时更新（确保实例存在）
                if (this.lazyLoadInstance) {
                    this.lazyLoadInstance.update();
                } else {
                    this.initLazyLoad();
                }
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
        }
        // 属性变化（如 lazy-loaded 类添加）由 CSS 自动处理光标状态，无需手动更新
    }
}

// 确保在 DOM 加载完成后执行
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.appInstance = new App();
    });
} else {
    window.appInstance = new App();
}

// Blazor 启动通知（由 MainLayout.OnAfterRenderAsync 调用）
window.notifyBlazorStarted = function() {
    if (window.appInstance && typeof window.appInstance.onBlazorStarted === 'function') {
        window.appInstance.onBlazorStarted();
    }
};

// 文件下载工具函数
window.downloadFile = function(fileName, base64Content, contentType) {
    try {
        // 将 base64 转换为 Blob
        const binaryString = window.atob(base64Content);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        const blob = new Blob([bytes], { type: contentType });
        
        // 创建下载链接
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        
        // 触发下载
        document.body.appendChild(link);
        link.click();
        
        // 清理
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    } catch (error) {
        console.error('Download failed:', error);
        throw error;
    }
};