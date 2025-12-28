import {CodeArea} from "./modules/code-area.js";
import {BackToTop} from "./modules/back-to-top.js";

class App {
    constructor() {
        this.init();
    }

    init() {
        // 初始化 LazyLoad 实例
        // 文档参考: https://github.com/verlok/vanilla-lazyload
        this.lazyLoadInstance = new LazyLoad({
            elements_selector: ".lazy",
            // 骨架屏效果相关类
            class_loading: "lazy-loading",
            class_loaded: "lazy-loaded",
            class_error: "lazy-error",
        });

        // 使用 MutationObserver 监听 DOM 变化
        this.observer = new MutationObserver((mutations) => {
            let hasNewNodes = false;
            let hasNewCodeBlocks = false;
            
            // 尝试初始化返回顶部 (如果之前未成功)
            if (!this.backToTopInstance) {
                this.initBackToTop();
            }

            for (const mutation of mutations) {
                // 检查是否有新增节点
                if (mutation.addedNodes.length > 0) {
                    hasNewNodes = true;
                    
                    // 检查新增节点中是否包含代码块 (pre code)
                    // 注意：mutation.addedNodes 是 NodeList，可能包含文本节点，需要过滤
                    for (const node of mutation.addedNodes) {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            // 如果插入的节点本身是 pre code 或者包含 pre code
                            if (node.querySelector && node.querySelector('pre code')) {
                                hasNewCodeBlocks = true;
                                break;
                            }
                            // 或者通过 class 判断，如果您的代码块容器有特定类名
                        }
                    }
                }
                
                // 也可以监听属性变化，如果 src 变化了 (针对 LazyLoad)
                if (mutation.type === 'attributes' && (mutation.target.classList.contains('lazy'))) {
                    hasNewNodes = true; 
                }
                
                if(hasNewNodes && hasNewCodeBlocks) break;
            }

            if (hasNewNodes) {
                this.lazyLoadInstance.update();
            }

            // 如果检测到新加入的代码块，重新触发 Prism 高亮
            if (hasNewCodeBlocks) {
                // 使用 requestAnimationFrame 确保在 DOM 渲染后执行
                // 或者简单的 setTimeout，因为 Prism 需要读取 DOM 内容
                setTimeout(() => {
                   new CodeArea();
                }, 0);
            }
        });

        // 观察整个 body 的子节点变化
        this.observer.observe(document.body, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['data-src', 'src'] // 仅监听相关属性
        });
        
        // 首次加载也运行一次
        new CodeArea();
    }

    initBackToTop() {
        if (document.getElementById('back-to-top')) {
            this.backToTopInstance = new BackToTop();
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