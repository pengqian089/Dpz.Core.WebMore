/**
 * DOM 变化管理器
 * 负责监听 DOM 变化并通知注册的处理器
 * 类似 C# 的事件机制
 */
export class DOMChangeManager {
    constructor() {
        this.handlers = [];
        this.pendingUpdate = false;
        this.paused = false; // 添加暂停标志
    }
    
    /**
     * 暂停 DOM 监听（例如在 PhotoSwipe 打开时）
     */
    pause() {
        this.paused = true;
    }
    
    /**
     * 恢复 DOM 监听
     */
    resume() {
        this.paused = false;
    }

    /**
     * 注册处理器
     * @param {Object} handler - 处理器对象
     * @param {string} handler.name - 处理器名称
     * @param {string[]} handler.selectors - 需要监听的 CSS 选择器
     * @param {Function} handler.onInit - 初始化时执行
     * @param {Function} handler.onUpdate - DOM 变化时执行
     */
    register(handler) {
        this.handlers.push(handler);
    }

    /**
     * 开始监听 DOM 变化
     */
    start() {
        // 首次初始化所有处理器
        this.handlers.forEach(handler => {
            if (handler.onInit) {
                handler.onInit();
            }
        });

        // 创建 MutationObserver
        this.observer = new MutationObserver((mutations) => {
            // 如果已暂停，跳过处理
            if (this.paused) {
                // console.log('[DOMChangeManager] 已暂停，跳过处理');
                return;
            }
            
            // 使用防抖机制，避免频繁触发
            // MutationObserver 本身会批量处理变化，但我们再加一层保护
            if (this.pendingUpdate) return;
            
            this.pendingUpdate = true;
            
            // 使用 requestAnimationFrame 比 setTimeout 更好
            // 它会在浏览器下次重绘前执行，确保 DOM 已经渲染
            requestAnimationFrame(() => {
                // 再次检查是否已暂停
                if (!this.paused) {
                    this.handleMutations(mutations);
                }
                this.pendingUpdate = false;
            });
        });

        // 开始观察
        this.observer.observe(document.body, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['data-src', 'src', 'class']
        });
    }

    /**
     * 处理 DOM 变化
     */
    handleMutations(mutations) {
        // 收集所有变化的节点
        const addedElements = new Set();
        const changedElements = new Set();

        for (const mutation of mutations) {
            // 处理新增节点
            if (mutation.addedNodes.length > 0) {
                for (const node of mutation.addedNodes) {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        addedElements.add(node);
                    }
                }
            }
            
            // 处理属性变化
            if (mutation.type === 'attributes' && mutation.target.nodeType === Node.ELEMENT_NODE) {
                changedElements.add(mutation.target);
            }
        }

        // 如果没有变化，直接返回
        if (addedElements.size === 0 && changedElements.size === 0) {
            return;
        }

        // 通知所有处理器检查是否需要处理
        this.handlers.forEach(handler => {
            if (!handler.onUpdate || !handler.selectors) return;

            let needsUpdate = false;

            // 检查新增的元素
            for (const element of addedElements) {
                if (this.matchesAnySelector(element, handler.selectors)) {
                    needsUpdate = true;
                    break;
                }
            }

            // 检查属性变化的元素（如果还没确定需要更新）
            if (!needsUpdate) {
                for (const element of changedElements) {
                    if (this.matchesAnySelector(element, handler.selectors)) {
                        needsUpdate = true;
                        break;
                    }
                }
            }

            // 调用处理器的更新方法
            if (needsUpdate) {
                handler.onUpdate();
            }
        });
    }

    /**
     * 检查元素是否匹配任一选择器
     * 会检查元素本身以及其子元素
     */
    matchesAnySelector(element, selectors) {
        for (const selector of selectors) {
            // 检查元素本身
            if (element.matches && element.matches(selector)) {
                return true;
            }
            // 检查子元素
            if (element.querySelector && element.querySelector(selector)) {
                return true;
            }
        }
        return false;
    }

    /**
     * 停止监听
     */
    stop() {
        if (this.observer) {
            this.observer.disconnect();
        }
    }
}
