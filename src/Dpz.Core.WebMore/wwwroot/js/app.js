class App {
    constructor() {
        this.init();
    }

    init() {
        // 初始化 LazyLoad 实例
        // 文档参考: https://github.com/verlok/vanilla-lazyload
        this.lazyLoadInstance = new LazyLoad({
            elements_selector: ".lazy",
            // 可以在这里配置淡入效果等
            // class_loading: "lzl-loading",
            // class_loaded: "lzl-loaded",
        });

        // 使用 MutationObserver 监听 DOM 变化
        this.observer = new MutationObserver((mutations) => {
            let hasNewNodes = false;
            
            for (const mutation of mutations) {
                if (mutation.addedNodes.length > 0) {
                    hasNewNodes = true;
                    break;
                }
                // 也可以监听属性变化，如果 src 变化了
                if (mutation.type === 'attributes' && (mutation.target.classList.contains('lazy'))) {
                    hasNewNodes = true; 
                    break;
                }
            }

            if (hasNewNodes) {
                this.lazyLoadInstance.update();
            }
        });

        // 观察整个 body 的子节点变化
        this.observer.observe(document.body, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['data-src', 'src'] // 仅监听相关属性
        });
    }
}

// 确保在 DOM 加载完成后执行
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => new App());
} else {
    new App();
}
