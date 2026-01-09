/**
 * CanvasChat 隔离 JavaScript 模块
 */

let resizeHandler = null;
let dotNetRef = null;

const STORAGE_KEY = 'canvasChat_brushColor';

export function initialize() {
    // 设置画布上下文属性
    const canvas = document.querySelector('.canvas-chat__canvas');
    if (canvas) {
        const ctx = canvas.getContext('2d');
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
    }
}

export function isDarkMode() {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

export function getDefaultColor() {
    // 先尝试从 localStorage 读取
    const savedColor = localStorage.getItem(STORAGE_KEY);
    if (savedColor) {
        return savedColor;
    }
    
    // 根据主题返回默认颜色
    return isDarkMode() ? '#FFFFFF' : '#000000';
}

export function saveColor(color) {
    try {
        localStorage.setItem(STORAGE_KEY, color);
    } catch (e) {
        console.warn('Failed to save brush color:', e);
    }
}

export function setupResizeListener(componentRef) {
    // 保存 .NET 引用
    dotNetRef = componentRef;
    
    // 创建防抖的 resize 处理器
    let resizeTimeout;
    resizeHandler = () => {
        if (resizeTimeout) {
            clearTimeout(resizeTimeout);
        }
        resizeTimeout = setTimeout(async () => {
            if (dotNetRef) {
                try {
                    await dotNetRef.invokeMethodAsync('OnWindowResized');
                } catch (e) {
                    console.error('Failed to invoke OnWindowResized:', e);
                }
            }
        }, 150);
    };
    
    window.addEventListener('resize', resizeHandler);
    window.addEventListener('orientationchange', resizeHandler);
    
    // 监听 Visual Viewport API（如果支持）
    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', resizeHandler);
    }
}

export function cleanupResizeListener() {
    if (resizeHandler) {
        window.removeEventListener('resize', resizeHandler);
        window.removeEventListener('orientationchange', resizeHandler);
        
        if (window.visualViewport) {
            window.visualViewport.removeEventListener('resize', resizeHandler);
        }
        
        resizeHandler = null;
        dotNetRef = null;
    }
}

export function getViewportSize() {
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.visualViewport 
        ? window.visualViewport.height 
        : window.innerHeight;
    
    const isMobile = viewportWidth <= 640;
    
    // 为头部和工具栏预留空间
    const headerHeight = 50; // 头部高度
    const toolbarHeight = 60; // 工具栏高度
    const padding = 20; // 内边距
    
    let width, height;
    
    if (isMobile) {
        // 移动端：尽可能利用全屏
        width = viewportWidth - padding;
        height = viewportHeight - headerHeight - toolbarHeight - padding;
    } else {
        // 桌面端：使用固定比例
        const maxWidth = 900;
        const maxHeight = 650;
        width = Math.min(viewportWidth * 0.85, maxWidth);
        height = Math.min(viewportHeight * 0.7, maxHeight) - headerHeight - toolbarHeight;
    }
    
    return {
        width: Math.floor(width),
        height: Math.floor(height)
    };
}

export function getCanvasPosition(clientX, clientY) {
    const canvas = document.querySelector('.canvas-chat__canvas');
    if (!canvas) {
        return { x: 0, y: 0 };
    }
    
    const rect = canvas.getBoundingClientRect();
    return {
        x: clientX - rect.left,
        y: clientY - rect.top
    };
}

export function drawLine(x0, y0, x1, y1, color, size) {
    const canvas = document.querySelector('.canvas-chat__canvas');
    if (!canvas) {
        return;
    }
    
    const ctx = canvas.getContext('2d');
    ctx.beginPath();
    ctx.moveTo(x0, y0);
    ctx.lineTo(x1, y1);
    ctx.strokeStyle = color;
    ctx.lineWidth = size;
    ctx.stroke();
    ctx.closePath();
}

export function clearCanvas() {
    const canvas = document.querySelector('.canvas-chat__canvas');
    if (!canvas) {
        return;
    }
    
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);
}
