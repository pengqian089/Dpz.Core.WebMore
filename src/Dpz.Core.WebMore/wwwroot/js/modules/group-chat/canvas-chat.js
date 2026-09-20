/**
 * 实时画图模块
 */

import { dialog } from '../dialog.js';
import { outPutError, outPutInfo, outPutSuccess } from '../console-logger.js';

class CanvasChat {
    constructor() {
        this.connection = null;
        this.isOpen = false;
        this.drawingUser = null;
        this.currentUserId = null;
        this.isDrawing = false;
        this.context = null;
        this.lastPos = { x: 0, y: 0 };
        this.color = '#000000';
        this.size = 2;
        this.canvasContainer = null;
        this.canvas = null;
        
        // 添加到DOM
        this.createUI();
    }

    init(connection) {
        this.connection = connection;
        
        if (!this.connection) return;

        this.connection.on("OnDrawingUserChanged", (user) => {
            this.handleDrawingUserChanged(user);
        });

        this.connection.on("OnDraw", (data) => {
            this.handleRemoteDraw(data);
        });
    }

    setUserId(userId) {
        this.currentUserId = userId;
    }

    createUI() {
        const container = document.createElement('div');
        container.className = 'canvas-chat';
        container.innerHTML = `
            <div class="canvas-chat__container">
                <div class="canvas-chat__header">
                    <div style="display:flex; align-items:center;">
                        <h3 class="canvas-chat__title">你画我猜 (实时画板)</h3>
                        <span class="canvas-chat__status" id="canvas-chat-status">等待开始...</span>
                    </div>
                    <button class="canvas-chat__close" aria-label="关闭">
                        <i class="fa fa-times"></i>
                    </button>
                </div>
                <div class="canvas-chat__body" id="canvas-chat-body">
                    <canvas class="canvas-chat__canvas" id="canvas-chat-canvas"></canvas>
                </div>
                <div class="canvas-chat__toolbar" id="canvas-chat-toolbar">
                    <input type="color" class="canvas-chat__color-picker" id="canvas-chat-color" value="#000000" title="选择颜色">
                    <input type="range" class="canvas-chat__size-slider" id="canvas-chat-size" min="1" max="20" value="2" title="笔刷大小">
                    <button class="canvas-chat__tool-btn" id="canvas-chat-clear" title="清空画板">
                        <i class="fa fa-trash"></i>
                    </button>
                    <button class="canvas-chat__tool-btn canvas-chat__tool-btn--active" id="canvas-chat-pencil" title="画笔">
                        <i class="fa fa-pencil"></i>
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(container);
        this.canvasContainer = container;
        this.canvas = container.querySelector('#canvas-chat-canvas');
        this.context = this.canvas.getContext('2d');
        this.statusEl = container.querySelector('#canvas-chat-status');
        this.toolbar = container.querySelector('#canvas-chat-toolbar');
        this.closeBtn = container.querySelector('.canvas-chat__close');
        
        this.colorPicker = container.querySelector('#canvas-chat-color');
        this.sizeSlider = container.querySelector('#canvas-chat-size');
        this.clearBtn = container.querySelector('#canvas-chat-clear');

        this.bindEvents();
        this.resizeCanvas();
        
        // 监听窗口大小变化
        window.addEventListener('resize', () => this.resizeCanvas());
        window.addEventListener('orientationchange', () => {
            setTimeout(() => this.resizeCanvas(), 100);
        });
        
        // 监听视口变化（Visual Viewport API，如果支持）
        if (window.visualViewport) {
            window.visualViewport.addEventListener('resize', () => this.resizeCanvas());
            window.visualViewport.addEventListener('scroll', () => this.resizeCanvas());
        }
    }

    bindEvents() {
        // 关闭按钮
        this.closeBtn.addEventListener('click', () => {
            this.close();
        });

        // 颜色选择
        this.colorPicker.addEventListener('change', (e) => {
            this.color = e.target.value;
        });

        // 大小选择
        this.sizeSlider.addEventListener('input', (e) => {
            this.size = parseInt(e.target.value);
        });

        // 清空
        this.clearBtn.addEventListener('click', () => {
            if (this.canDraw()) {
                this.clearCanvas();
                this.sendDrawData({ type: 'clear' });
            }
        });

        // 画布事件
        const startDrawing = (e) => {
            if (!this.canDraw()) return;
            this.isDrawing = true;
            const pos = this.getPos(e);
            this.lastPos = pos;
            
            // 发送开始点，确保单点也能画出来
            this.draw(this.lastPos, pos, this.color, this.size);
            this.sendDrawData({
                type: 'path',
                x0: this.lastPos.x / this.canvas.width,
                y0: this.lastPos.y / this.canvas.height,
                x1: pos.x / this.canvas.width,
                y1: pos.y / this.canvas.height,
                color: this.color,
                size: this.size
            });
        };

        const draw = (e) => {
            if (!this.isDrawing || !this.canDraw()) return;
            e.preventDefault(); // 防止触摸滚动
            
            const pos = this.getPos(e);
            
            // 绘制
            this.draw(this.lastPos, pos, this.color, this.size);
            
            // 发送数据 (归一化坐标)
            this.sendDrawData({
                type: 'path',
                x0: this.lastPos.x / this.canvas.width,
                y0: this.lastPos.y / this.canvas.height,
                x1: pos.x / this.canvas.width,
                y1: pos.y / this.canvas.height,
                color: this.color,
                size: this.size
            });

            this.lastPos = pos;
        };

        const stopDrawing = () => {
            this.isDrawing = false;
        };

        // Mouse events
        this.canvas.addEventListener('mousedown', startDrawing);
        this.canvas.addEventListener('mousemove', draw);
        this.canvas.addEventListener('mouseup', stopDrawing);
        this.canvas.addEventListener('mouseout', stopDrawing);

        // Touch events
        this.canvas.addEventListener('touchstart', (e) => {
            if (e.touches.length === 1) startDrawing(e.touches[0]);
        }, { passive: false });
        this.canvas.addEventListener('touchmove', (e) => {
            if (e.touches.length === 1) draw(e.touches[0]);
        }, { passive: false });
        this.canvas.addEventListener('touchend', stopDrawing);
    }

    getPos(e) {
        const rect = this.canvas.getBoundingClientRect();
        return {
            x: e.clientX - rect.left,
            y: e.clientY - rect.top
        };
    }

    resizeCanvas() {
        if (!this.canvasContainer) return;
        
        // 获取真实视口高度（移动端考虑地址栏）
        const getViewportHeight = () => {
            // Visual Viewport API
            if (window.visualViewport) {
                return window.visualViewport.height;
            }
            // 回退到 window.innerHeight
            return window.innerHeight;
        };

        const viewportHeight = getViewportHeight();
        const viewportWidth = window.innerWidth;
        
        // 移动端使用更大的画布区域
        const isMobile = viewportWidth <= 640;
        const width = isMobile 
            ? Math.min(viewportWidth * 0.95, viewportWidth - 20) 
            : Math.min(viewportWidth * 0.9, 800);
        const height = isMobile 
            ? Math.min(viewportHeight * 0.85, viewportHeight - 100) 
            : Math.min(viewportHeight * 0.7, 600);
        
        // 设置canvas显示大小
        this.canvas.style.width = `${width}px`;
        this.canvas.style.height = `${height}px`;
        
        // 设置canvas实际大小 (考虑DPI)
        // 简单起见，这里直接使用显示大小，如果需要高清，可以 * devicePixelRatio
        this.canvas.width = width;
        this.canvas.height = height;
        
        // 重设Context属性 (因为重设宽高会重置Context)
        this.context.lineCap = 'round';
        this.context.lineJoin = 'round';
    }

    canDraw() {
        return this.isOpen && this.drawingUser && this.currentUserId && this.drawingUser.id === this.currentUserId;
    }

    async requestAccess() {
        if (!this.connection) return;
        try {
            await this.connection.invoke("RequestDrawingAccess");
        } catch (err) {
            outPutError("请求画图权限失败: " + err);
        }
    }

    async releaseAccess() {
        if (!this.connection) return;
        try {
            await this.connection.invoke("ReleaseDrawingAccess");
        } catch (err) {
            outPutError("释放画图权限失败: " + err);
        }
    }

    async sendDrawData(data) {
        if (!this.connection) return;
        try {
            await this.connection.invoke("SendDrawingData", data);
        } catch (err) {
            console.error("发送画图数据失败", err);
        }
    }

    handleDrawingUserChanged(user) {
        this.drawingUser = user;
        
        if (user) {
            if (user.id === this.currentUserId) {
                this.statusEl.textContent = "正在作画：你自己";
                this.statusEl.style.color = "#2196f3";
                this.toolbar.style.display = 'flex';
                dialog.toast('你已获得画图权限，开始展示吧！', 'success');
                
                if (!this.isOpen) {
                    this.open();
                }
            } else {
                this.statusEl.textContent = `正在作画：${user.name}`;
                this.statusEl.style.color = "#666";
                this.toolbar.style.display = 'none'; // 观看者不能操作工具栏
                
                // 自动打开观看
                if (!this.isOpen) {
                    this.open();
                    dialog.toast(`${user.name} 正在作画，已自动打开画板`, 'info');
                } else {
                    dialog.toast(`${user.name} 开始作画了`, 'info');
                }
            }
        } else {
            this.statusEl.textContent = "画板空闲";
            this.statusEl.style.color = "#666";
            this.toolbar.style.display = 'none';
            
            if (this.isOpen) {
                dialog.toast('画图已结束', 'info');
                this.close();
            }
        }
    }

    handleRemoteDraw(data) {
        if (!this.isOpen) return;

        if (data.type === 'clear') {
            this.clearCanvas();
        } else if (data.type === 'path') {
            const x0 = data.x0 * this.canvas.width;
            const y0 = data.y0 * this.canvas.height;
            const x1 = data.x1 * this.canvas.width;
            const y1 = data.y1 * this.canvas.height;
            this.draw({x: x0, y: y0}, {x: x1, y: y1}, data.color, data.size);
        }
    }

    draw(start, end, color, size) {
        this.context.beginPath();
        this.context.moveTo(start.x, start.y);
        this.context.lineTo(end.x, end.y);
        this.context.strokeStyle = color;
        this.context.lineWidth = size;
        this.context.stroke();
        this.context.closePath();
    }

    clearCanvas() {
        this.context.clearRect(0, 0, this.canvas.width, this.canvas.height);
    }

    open() {
        this.isOpen = true;
        this.canvasContainer.classList.add('canvas-chat--visible');
        // 确保视口高度已更新
        const vh = window.innerHeight * 0.01;
        document.documentElement.style.setProperty('--vh', `${vh}px`);
        // 延迟一下再调整画布，确保布局已完成
        requestAnimationFrame(() => {
            this.resizeCanvas();
        });
    }

    close() {
        this.isOpen = false;
        this.canvasContainer.classList.remove('canvas-chat--visible');
        
        // 如果是我在画，关闭时释放权限
        if (this.drawingUser && this.currentUserId && this.drawingUser.id === this.currentUserId) {
            this.releaseAccess();
        }
    }
}

export const canvasChat = new CanvasChat();

