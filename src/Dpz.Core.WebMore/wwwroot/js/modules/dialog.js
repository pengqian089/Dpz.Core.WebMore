/**
 * 对话框与通知系统
 * 现代化的、基于 Promise 的 layer.js 替代方案
 */

class DialogUI {
    constructor() {
        this.initToastContainer();
        // 通知框实例集合
        this._notificationInstances = new Set();
    }

    initToastContainer() {
        if (!document.querySelector('.toast-container')) {
            const container = document.createElement('div');
            container.className = 'toast-container';
            document.body.appendChild(container);
        }
    }

    /**
     * 创建通用的遮罩层和对话框结构
     * @param {string} title - 标题
     * @param {string} content - 内容
     * @param {string} type - 类型：'alert' | 'confirm' | 'prompt'
     * @returns {Object} 包含 overlay 和 dialog 的对象
     */
    createDialog(title, content, type = 'alert') {
        const overlay = document.createElement('div');
        overlay.className = 'dialog-overlay';
        
        const dialog = document.createElement('div');
        dialog.className = 'dialog';
        
        // 头部
        const header = document.createElement('div');
        header.className = 'dialog__header';
        header.innerHTML = `
            <span>${title || '提示'}</span>
            <button type="button" class="dialog__close" aria-label="关闭">
                <i class="fa fa-times"></i>
            </button>
        `;
        
        // 主体
        const body = document.createElement('div');
        body.className = 'dialog__body';
        body.innerHTML = `<div class="dialog__message">${content}</div>`;
        
        if (type === 'prompt') {
            const inputWrapper = document.createElement('div');
            inputWrapper.className = 'dialog__input-wrapper';
            inputWrapper.innerHTML = `<input type="text" class="dialog__input" autocomplete="off">`;
            body.appendChild(inputWrapper);
        }

        // 底部
        const footer = document.createElement('div');
        footer.className = 'dialog__footer';
        
        if (type === 'alert') {
            footer.innerHTML = `
                <button type="button" class="dialog__btn dialog__btn--primary" data-action="confirm">确定</button>
            `;
        } else if (type === 'confirm' || type === 'prompt') {
            footer.innerHTML = `
                <button type="button" class="dialog__btn dialog__btn--default" data-action="cancel">取消</button>
                <button type="button" class="dialog__btn dialog__btn--primary" data-action="confirm">确定</button>
            `;
        }

        dialog.appendChild(header);
        dialog.appendChild(body);
        dialog.appendChild(footer);
        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        // 强制重排以触发动画
        overlay.offsetHeight; 
        overlay.classList.add('dialog-overlay--visible');

        return { overlay, dialog };
    }

    closeDialog(overlay) {
        overlay.classList.remove('dialog-overlay--visible');
        overlay.addEventListener('transitionend', () => {
            if (overlay.parentNode) {
                overlay.parentNode.removeChild(overlay);
            }
        }, { once: true });
    }

    /**
     * 显示模态提示框
     * @param {string} message - 消息内容
     * @param {string} title - 标题
     * @returns {Promise<void>}
     */
    alert(message, title = '提示') {
        return new Promise((resolve) => {
            const { overlay, dialog } = this.createDialog(title, message, 'alert');
            
            const handleClose = () => {
                this.closeDialog(overlay);
                resolve();
            };

            dialog.querySelector('[data-action="confirm"]').addEventListener('click', handleClose);
            dialog.querySelector('.dialog__close').addEventListener('click', handleClose);
            
            // 聚焦主要按钮
            dialog.querySelector('.dialog__btn--primary').focus();
        });
    }

    /**
     * 显示模态确认框
     * @param {string} message - 消息内容
     * @param {string} title - 标题
     * @returns {Promise<boolean>} 用户点击确定返回 true，取消返回 false
     */
    confirm(message, title = '确认') {
        return new Promise((resolve) => {
            const { overlay, dialog } = this.createDialog(title, message, 'confirm');
            
            const handleConfirm = () => {
                this.closeDialog(overlay);
                resolve(true);
            };
            
            const handleCancel = () => {
                this.closeDialog(overlay);
                resolve(false);
            };

            dialog.querySelector('[data-action="confirm"]').addEventListener('click', handleConfirm);
            dialog.querySelector('[data-action="cancel"]').addEventListener('click', handleCancel);
            dialog.querySelector('.dialog__close').addEventListener('click', handleCancel);
            
            dialog.querySelector('.dialog__btn--primary').focus();
        });
    }

    /**
     * 显示模态输入框
     * @param {string} message - 提示消息
     * @param {string} title - 标题
     * @param {string} defaultValue - 默认值
     * @returns {Promise<string|null>} 用户输入的值，取消返回 null
     */
    prompt(message, title = '输入', defaultValue = '') {
        return new Promise((resolve) => {
            const { overlay, dialog } = this.createDialog(title, message, 'prompt');
            const input = dialog.querySelector('.dialog__input');
            input.value = defaultValue;
            
            const handleConfirm = () => {
                const val = input.value;
                this.closeDialog(overlay);
                resolve(val);
            };
            
            const handleCancel = () => {
                this.closeDialog(overlay);
                resolve(null);
            };

            dialog.querySelector('[data-action="confirm"]').addEventListener('click', handleConfirm);
            dialog.querySelector('[data-action="cancel"]').addEventListener('click', handleCancel);
            dialog.querySelector('.dialog__close').addEventListener('click', handleCancel);
            
            // 处理输入框的 Enter 和 Escape 键
            input.addEventListener('keydown', (e) => {
                if (e.key === 'Enter') handleConfirm();
                if (e.key === 'Escape') handleCancel();
            });

            setTimeout(() => input.focus(), 50);
        });
    }

    /**
     * 显示 Toast 消息
     * @param {string} message - 消息内容
     * @param {string} type - 类型：'info' | 'success' | 'warning' | 'error'
     * @param {number} duration - 显示时长（毫秒）
     */
    toast(message, type = 'info', duration = 3000) {
        this.initToastContainer();
        const container = document.querySelector('.toast-container');
        
        const toast = document.createElement('div');
        toast.className = `toast toast--${type}`;
        
        let iconClass = 'fa-info-circle';
        if (type === 'success') iconClass = 'fa-check-circle';
        if (type === 'warning') iconClass = 'fa-exclamation-triangle';
        if (type === 'error') iconClass = 'fa-times-circle';

        toast.innerHTML = `
            <i class="toast__icon fa ${iconClass}"></i>
            <span class="toast__content">${message}</span>
        `;
        
        container.appendChild(toast);

        setTimeout(() => {
            toast.classList.add('is-leaving');
            toast.addEventListener('animationend', () => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            });
        }, duration);
    }

    /**
     * 显示工具提示
     * @param {HTMLElement} element - 目标元素
     * @param {string} text - 提示内容
     */
    showTooltip(element, text) {
        if (!text) return;
        
        let tooltip = document.getElementById('dialog-tooltip');
        if (!tooltip) {
            tooltip = document.createElement('div');
            tooltip.id = 'dialog-tooltip';
            tooltip.className = 'dialog-tooltip';
            document.body.appendChild(tooltip);
        }

        // 清除旧的检测定时器
        if (this._tooltipTimer) {
            clearInterval(this._tooltipTimer);
            this._tooltipTimer = null;
        }

        tooltip.textContent = text;
        
        // 在计算位置前重置状态
        tooltip.classList.remove('is-bottom');
        tooltip.classList.add('is-visible');

        const updatePosition = () => {
            if (!element || !document.body.contains(element)) {
                this.hideTooltip();
                return;
            }

            const rect = element.getBoundingClientRect();
            // 如果元素不可见（不在视口内或 display:none），隐藏 tooltip
            if (rect.width === 0 && rect.height === 0) {
                this.hideTooltip();
                return;
            }

            const tooltipRect = tooltip.getBoundingClientRect();
            
            // 默认位置：顶部居中
            let top = rect.top - tooltipRect.height - 8; // 8px 间距
            let left = rect.left + (rect.width - tooltipRect.width) / 2;

            // 检查顶部边界
            if (top < 0) {
                // 翻转到底部
                top = rect.bottom + 8;
                tooltip.classList.add('is-bottom');
            } else {
                 tooltip.classList.remove('is-bottom');
            }

            // 检查左右边界
            if (left < 0) {
                left = 4; // 距离左边缘的边距
            } else if (left + tooltipRect.width > window.innerWidth) {
                left = window.innerWidth - tooltipRect.width - 4; // 距离右边缘的边距
            }

            tooltip.style.top = `${top}px`;
            tooltip.style.left = `${left}px`;
        };

        updatePosition();

        // 启动定时器检测元素状态（每 200ms 检查一次）
        this._tooltipTimer = setInterval(updatePosition, 200);
    }

    /**
     * 隐藏工具提示
     */
    hideTooltip() {
        const tooltip = document.getElementById('dialog-tooltip');
        if (tooltip) {
            tooltip.classList.remove('is-visible');
        }
        if (this._tooltipTimer) {
            clearInterval(this._tooltipTimer);
            this._tooltipTimer = null;
        }
    }

    // ========== 通知框相关方法 ==========

    /**
     * 显示通知框
     * @typedef {Object} NotificationOptions
     * @property {string} [title=""] - 通知框标题
     * @property {string} [content=""] - 通知框内容
     * @property {number[]} [bars=[]] - 进度条数组
     * @property {number} [autoClose=0] - 自动关闭时间（毫秒），0 表示不自动关闭
     * @property {string} [type="info"] - 通知类型：info/success/warning/error
     * @param {NotificationOptions} option - 通知选项
     * @returns {HTMLElement} 通知框 DOM 元素
     */
    showNotification(option = {}) {
        const setting = {
            title: "",
            content: "",
            bars: [],
            autoClose: 0,
            type: "info",
            ...option
        };

        // 计算位置
        const top = this._calculateNotificationTopPosition();
        
        // 创建通知框
        const box = this._createNotificationBox(setting);
        box.style.top = `${top}px`;
        
        // 添加到 DOM 并跟踪实例
        document.body.appendChild(box);
        this._notificationInstances.add(box);
        
        // 设置自动关闭
        if (setting.autoClose > 0) {
            setTimeout(() => this.closeNotification(box), setting.autoClose);
        }
        
        // 添加入场动画
        requestAnimationFrame(() => {
            box.classList.add('notification-box--enter');
        });

        return box;
    }

    /**
     * 设置通知框内容
     * @param {HTMLElement} box - 通知框元素
     * @param {string} content - 新内容
     */
    setNotificationContent(box, content) {
        if (!this._validateNotificationBox(box)) return;
        
        const contentEl = box.querySelector('.notification-box__content');
        if (contentEl) {
            contentEl.textContent = String(content || "");
        }
    }

    /**
     * 设置通知框标题
     * @param {HTMLElement} box - 通知框元素
     * @param {string} title - 新标题
     */
    setNotificationTitle(box, title) {
        if (!this._validateNotificationBox(box)) return;
        
        const titleEl = box.querySelector('.notification-box__title');
        if (titleEl) {
            titleEl.textContent = String(title || "");
        }
    }

    /**
     * 设置进度条的进度
     * @param {HTMLElement} box - 通知框元素
     * @param {number[]} values - 进度值数组
     */
    setNotificationProgress(box, values) {
        if (!this._validateNotificationBox(box) || !Array.isArray(values)) return;

        const progressContainers = box.querySelectorAll('.notification-box__progress');
        
        values.forEach((value, index) => {
            if (typeof value === "number" && index < progressContainers.length) {
                const clampedValue = Math.max(0, Math.min(100, value));
                const progress = progressContainers[index];
                const track = progress.querySelector('.notification-box__progress-track');
                const bar = progress.querySelector('.notification-box__progress-bar');
                const textValue = `${clampedValue.toFixed(1)}%`;
                
                // 更新进度条宽度和显示状态
                if (bar) {
                    if (clampedValue > 0) {
                        bar.style.width = `${clampedValue}%`;
                        bar.style.display = '';
                    } else {
                        bar.style.width = '0%';
                        bar.style.display = 'none';
                    }
                }
                
                // 更新百分比文字（始终显示在轨道中间）
                const textInternal = track ? track.querySelector('.notification-box__progress-text') : null;
                const textExternal = progress.querySelector('.notification-box__progress-text--external');
                
                if (textInternal) {
                    // 如果已经有内部文字，直接更新
                    textInternal.textContent = textValue;
                } else if (textExternal) {
                    // 如果之前是外部文字，移动到内部（中间）
                    textExternal.remove();
                    const newText = document.createElement('span');
                    newText.className = 'notification-box__progress-text';
                    newText.textContent = textValue;
                    if (track) {
                        track.appendChild(newText);
                    }
                } else if (track) {
                    // 如果都没有，创建新的
                    const newText = document.createElement('span');
                    newText.className = 'notification-box__progress-text';
                    newText.textContent = textValue;
                    track.appendChild(newText);
                }
            }
        });
    }

    /**
     * 关闭通知框
     * @param {HTMLElement} box - 要关闭的通知框
     */
    closeNotification(box) {
        if (!this._validateNotificationBox(box)) return;

        // 添加退场动画
        box.classList.add('notification-box--exit');
        
        // 动画完成后移除
        setTimeout(() => {
            if (box.parentNode) {
                this._notificationInstances.delete(box);
                box.parentNode.removeChild(box);
                this._recalculateNotificationPositions();
            }
        }, 300);
    }

    /**
     * 关闭所有通知框
     */
    closeAllNotifications() {
        const boxes = Array.from(this._notificationInstances);
        boxes.forEach(box => {
            this.closeNotification(box);
        });
    }

    /**
     * 获取当前活跃的通知框数量
     * @returns {number}
     */
    getNotificationCount() {
        return this._notificationInstances.size;
    }

    // ========== 通知框私有方法 ==========

    /**
     * 验证通知框元素
     * @private
     * @param {HTMLElement} box - 通知框元素
     * @returns {boolean}
     */
    _validateNotificationBox(box) {
        return box && box.parentNode && this._notificationInstances.has(box);
    }

    /**
     * 计算顶部位置
     * @private
     * @returns {number}
     */
    _calculateNotificationTopPosition() {
        let top = 15;
        this._notificationInstances.forEach(box => {
            if (box.offsetParent !== null) {
                top += box.offsetHeight + 15;
            }
        });
        return top;
    }

    /**
     * 重新计算所有通知框位置
     * @private
     */
    _recalculateNotificationPositions() {
        let currentTop = 15;
        this._notificationInstances.forEach(box => {
            if (box.offsetParent !== null) {
                box.style.top = `${currentTop}px`;
                currentTop += box.offsetHeight + 15;
            }
        });
    }

    /**
     * 创建通知框 DOM
     * @private
     * @param {Object} setting - 设置对象
     * @returns {HTMLElement}
     */
    _createNotificationBox(setting) {
        const box = document.createElement('div');
        box.className = `notification-box notification-box--${setting.type}`;
        
        // 创建标题
        if (setting.title) {
            const title = document.createElement('div');
            title.className = 'notification-box__title';
            title.textContent = setting.title;
            box.appendChild(title);
        }
        
        // 创建内容容器
        const container = document.createElement('div');
        container.className = 'notification-box__content-container';
        
        // 创建内容
        const content = document.createElement('div');
        content.className = 'notification-box__content';
        content.textContent = setting.content;
        container.appendChild(content);
        
        // 创建进度条
        this._createNotificationProgressBars(container, setting.bars);
        
        box.appendChild(container);
        
        // 添加关闭按钮
        this._addNotificationCloseButton(box);
        
        return box;
    }

    /**
     * 创建进度条
     * @private
     * @param {HTMLElement} container - 容器元素
     * @param {number[]} bars - 进度值数组
     */
    _createNotificationProgressBars(container, bars) {
        if (!Array.isArray(bars) || bars.length === 0) return;
        
        bars.forEach(value => {
            if (typeof value === "number") {
                const clampedValue = Math.max(0, Math.min(100, value));
                const progress = document.createElement('div');
                progress.className = 'notification-box__progress';
                
                // 创建进度条轨道
                const track = document.createElement('div');
                track.className = 'notification-box__progress-track';
                
                // 创建进度条
                const bar = document.createElement('div');
                bar.className = 'notification-box__progress-bar';
                if (clampedValue > 0) {
                    bar.style.width = `${clampedValue}%`;
                    bar.style.display = '';
                } else {
                    bar.style.width = '0%';
                    bar.style.display = 'none';
                }
                
                // 创建百分比文字（始终显示在进度条轨道中间）
                const textValue = `${clampedValue.toFixed(1)}%`;
                const text = document.createElement('span');
                text.className = 'notification-box__progress-text';
                text.textContent = textValue;
                
                // 文字始终添加到 track 中，使用绝对定位固定在中间
                track.appendChild(bar);
                track.appendChild(text);
                progress.appendChild(track);
                
                container.appendChild(progress);
            }
        });
    }

    /**
     * 添加关闭按钮
     * @private
     * @param {HTMLElement} box - 通知框元素
     */
    _addNotificationCloseButton(box) {
        const closeBtn = document.createElement('button');
        closeBtn.className = 'notification-box__close';
        closeBtn.innerHTML = '&times;';
        closeBtn.setAttribute('aria-label', '关闭通知');
        closeBtn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            this.closeNotification(box);
        });
        
        box.appendChild(closeBtn);
    }
}

export const dialog = new DialogUI();

