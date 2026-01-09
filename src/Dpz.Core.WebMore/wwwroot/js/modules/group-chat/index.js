/**
 * 群组聊天模块
 * 彩蛋功能：通过控制台开启
 */

import {dialog} from '../dialog.js';
import {outPutInfo, outPutError, outPutSuccess} from '../console-logger.js';
import {canvasChat} from './canvas-chat.js';

class GroupChat {
    constructor() {
        this.connection = null;
        this.isOpen = false;
        this.currentUserId = null;
        this.loadedPageIndex = 0;
        this.hasMoreHistory = true;
        this.isLoadingHistory = false;
        this.maxHistoryCount = 1000;
        this.loadedHistoryCount = 0;
        this.MOBILE_BREAKPOINT = 768;
        this.CHAT_HASH = '#group-chat';
        this.init();
        this.initMobileViewport();
        this.initHashListener();
    }

    init() {
        // 创建触发按钮（隐藏，通过控制台显示）
        this.createTriggerButton();

        // 监听控制台命令
        this.setupConsoleCommand();
    }

    initHashListener() {
        // 监听 hash 变化（浏览器前进后退）
        window.addEventListener('hashchange', () => {
            if (this.isMobile() && this.isOpen && window.location.hash !== this.CHAT_HASH) {
                // 移动端：hash被清除，关闭群聊
                this.close();
            }
        });
    }

    isMobile() {
        return window.innerWidth <= this.MOBILE_BREAKPOINT;
    }

    disableBodyScroll() {
        if (this.isMobile()) {
            document.body.style.overflow = 'hidden';
            document.body.style.position = 'fixed';
            document.body.style.width = '100%';
            document.body.style.top = `-${window.scrollY}px`;
            this.scrollPosition = window.scrollY;
        }
    }

    enableBodyScroll() {
        if (this.isMobile()) {
            document.body.style.overflow = '';
            document.body.style.position = '';
            document.body.style.width = '';
            document.body.style.top = '';
            if (this.scrollPosition !== undefined) {
                window.scrollTo(0, this.scrollPosition);
                this.scrollPosition = undefined;
            }
        }
    }

    initMobileViewport() {
        // 设置 CSS 自定义属性来处理移动端视口高度
        const setViewportHeight = () => {
            // 获取真实视口高度（不包括地址栏等）
            const vh = window.innerHeight * 0.01;
            document.documentElement.style.setProperty('--vh', `${vh}px`);
        };

        // 初始设置
        setViewportHeight();

        // 监听窗口大小变化和方向变化
        window.addEventListener('resize', setViewportHeight);
        window.addEventListener('orientationchange', () => {
            setTimeout(setViewportHeight, 100);
        });

        // iOS Safari 特殊处理：监听滚动事件来调整视口
        if (/iPhone|iPad|iPod/.test(navigator.userAgent)) {
            let lastScrollTop = 0;
            window.addEventListener('scroll', () => {
                const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
                if (scrollTop !== lastScrollTop) {
                    setViewportHeight();
                    lastScrollTop = scrollTop;
                }
            }, { passive: true });
        }

        // 监听视口变化（Visual Viewport API，如果支持）
        if (window.visualViewport) {
            window.visualViewport.addEventListener('resize', setViewportHeight);
            window.visualViewport.addEventListener('scroll', setViewportHeight);
        }
    }

    createTriggerButton() {
        const trigger = document.createElement('button');
        trigger.className = 'group-chat__trigger';
        trigger.innerHTML = '💬';
        trigger.setAttribute('aria-label', '打开群组聊天');
        trigger.style.display = 'none';
        trigger.addEventListener('click', () => this.open());
        document.body.appendChild(trigger);
        this.triggerButton = trigger;
    }

    setupConsoleCommand() {
        // 在控制台输入 window.openGroupChat() 来开启
        window.openGroupChat = () => {
            if (!this.isOpen) {
                this.open().then(() => outPutSuccess('群组聊天已打开'));
            } else {
                outPutInfo('群组聊天已经打开');
            }
        };

        // 显示提示
        outPutInfo('💬 群组聊天');
        outPutInfo('输入 window.openGroupChat() 来开启群组聊天');
    }

    async open() {
        if (this.isOpen) return;

        this.isOpen = true;
        
        // 移动端：添加 hash 和禁用滚动
        if (this.isMobile()) {
            // 避免触发 hashchange 事件导致立即关闭
            if (window.location.hash !== this.CHAT_HASH) {
                window.location.hash = this.CHAT_HASH;
            }
            this.disableBodyScroll();
        }
        
        this.createUI();
        await this.connect();
    }

    createUI() {
        // 创建聊天面板
        const chatPanel = document.createElement('div');
        chatPanel.className = 'group-chat';
        chatPanel.innerHTML = `
            <div class="group-chat__header">
                <h3 class="group-chat__title">群组聊天</h3>
                <button class="group-chat__close" aria-label="关闭">
                    <i class="fa fa-times"></i>
                </button>
            </div>
            <div class="group-chat__messages" id="group-chat-messages">
                <div class="group-chat__load-more" id="group-chat-load-more" style="display: none;">
                    <span>加载更多...</span>
                </div>
            </div>
            <div class="group-chat__input-area">
                <div class="group-chat__commands" id="group-chat-commands">
                    <div class="group-chat__command-item" data-command="/canvas">
                        <span class="group-chat__command-name">/canvas</span>
                        <span class="group-chat__command-desc">开启/加入 你画我猜</span>
                    </div>
                </div>
                <textarea 
                    class="group-chat__input" 
                    id="group-chat-input" 
                    placeholder="输入消息... (/ 显示命令)"
                    rows="1"
                ></textarea>
                <button class="group-chat__send-btn" id="group-chat-send">发送</button>
            </div>
        `;

        document.body.appendChild(chatPanel);
        this.chatPanel = chatPanel;
        this.messagesContainer = chatPanel.querySelector('#group-chat-messages');
        this.loadMoreBtn = chatPanel.querySelector('#group-chat-load-more');
        this.input = chatPanel.querySelector('#group-chat-input');
        this.sendBtn = chatPanel.querySelector('#group-chat-send');
        this.commandsPanel = chatPanel.querySelector('#group-chat-commands');

        // 绑定事件
        this.bindEvents();

        // 显示动画
        requestAnimationFrame(() => {
            chatPanel.classList.add('group-chat--visible');
            // 确保视口高度已更新
            const vh = window.innerHeight * 0.01;
            document.documentElement.style.setProperty('--vh', `${vh}px`);
        });

        // 隐藏触发按钮
        if (this.triggerButton) {
            this.triggerButton.style.display = 'none';
        }
    }

    bindEvents() {
        // 关闭按钮
        this.chatPanel.querySelector('.group-chat__close').addEventListener('click', async () => {
            await this.close();
        });

        // 发送按钮
        this.sendBtn.addEventListener('click', async () => {
            await this.handleSend();
        });

        // 输入框回车发送（Shift+Enter换行）
        this.input.addEventListener('keydown', async (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                await this.handleSend();
            }
        });

        // 自动调整输入框高度 & 命令提示
        this.input.addEventListener('input', () => {
            this.input.style.height = 'auto';
            this.input.style.height = `${Math.min(this.input.scrollHeight, 120)}px`;
            
            const val = this.input.value;
            if (val.startsWith('/')) {
                this.commandsPanel.classList.add('group-chat__commands--visible');
            } else {
                this.commandsPanel.classList.remove('group-chat__commands--visible');
            }
        });

        // 命令点击
        this.commandsPanel.querySelectorAll('.group-chat__command-item').forEach(item => {
            item.addEventListener('click', () => {
                const cmd = item.dataset.command;
                this.input.value = cmd;
                this.commandsPanel.classList.remove('group-chat__commands--visible');
                this.input.focus();
            });
        });

        // 加载更多
        this.loadMoreBtn.addEventListener('click', async () => {
            await this.loadMoreHistory();
        });

        // 滚动加载历史记录
        this.messagesContainer.addEventListener('scroll', async () => {
            if (this.messagesContainer.scrollTop === 0 &&
                this.hasMoreHistory &&
                !this.isLoadingHistory &&
                this.loadedHistoryCount < this.maxHistoryCount) {
                await this.loadMoreHistory();
            }
        });
    }

    async handleSend() {
        const content = this.input.value.trim();
        if (!content) return;

        if (content.startsWith('/')) {
            await this.handleCommand(content);
        } else {
            await this.sendMessage();
        }
    }

    async handleCommand(cmd) {
        if (cmd === '/canvas') {
            this.input.value = '';
            this.input.style.height = 'auto';
            this.commandsPanel.classList.remove('group-chat__commands--visible');
            
            canvasChat.open();
            if (!canvasChat.drawingUser) {
                 await canvasChat.requestAccess();
            }
        } else {
            // 未知命令，当做普通消息发送
            await this.sendMessage();
        }
    }

    async connect() {
        try {
            this.connection = new signalR
                .HubConnectionBuilder()
                .withUrl("/groupchat", {
                    skipNegotiation: true,
                    transport: signalR.HttpTransportType.WebSockets,
                })
                .withAutomaticReconnect()
                .build();

            // 注册客户端方法
            this.connection.on("OnMessageReceived", (message) => {
                this.addMessage(message, message.userId === this.currentUserId);
            });

            this.connection.on("OnUserJoined", (user) => {
                this.addSystemMessage(`${user.name} 加入了群组`);
            });

            this.connection.on("OnUserLeft", (user) => {
                this.addSystemMessage(`${user.name} 离开了群组`);
            });

            this.connection.on("OnHistoryLoaded", (messages, hasMore) => {
                this.handleHistoryLoaded(messages, hasMore);
            });

            this.connection.on("OnError", (error) => {
                dialog.toast(error, 'error');
            });

            // 连接
            await this.connection.start();

            // 加入群组
            const currentUser = await this.connection.invoke("JoinGroup",null);
            if (currentUser) {
                this.currentUserId = currentUser.id;
                canvasChat.setUserId(this.currentUserId);
            }
            
            canvasChat.init(this.connection);

            // 加载历史记录
            await this.loadInitialHistory();

            outPutSuccess('已连接到群组聊天');
            dialog.toast('已连接到群组聊天', 'success');
        } catch (error) {
            outPutError(`连接群组聊天失败: ${error.message || error}`);
            dialog.toast('连接失败，请刷新页面重试', 'error');
        }
    }

    async loadInitialHistory() {
        this.loadedPageIndex = 1;
        this.loadedHistoryCount = 0;
        this.hasMoreHistory = true;

        // 先加载第一页
        await this.connection.invoke("GetHistory", 1, 50);
    }

    async loadMoreHistory() {
        if (this.isLoadingHistory || !this.hasMoreHistory ||
            this.loadedHistoryCount >= this.maxHistoryCount) {
            return;
        }

        this.isLoadingHistory = true;
        this.loadMoreBtn.style.display = 'block';
        this.loadMoreBtn.classList.add('group-chat__load-more--loading');
        this.loadMoreBtn.querySelector('span').textContent = '加载中...';

        try {
            const nextPage = this.loadedPageIndex + 1;
            const scrollHeight = this.messagesContainer.scrollHeight;
            const scrollTop = this.messagesContainer.scrollTop;

            await this.connection.invoke("GetHistory", nextPage, 50);

            // 恢复滚动位置
            requestAnimationFrame(() => {
                const newScrollHeight = this.messagesContainer.scrollHeight;
                this.messagesContainer.scrollTop = newScrollHeight - scrollHeight + scrollTop;
            });
        } catch (error) {
            outPutError(`加载历史记录失败: ${error.message || error}`);
            dialog.toast('加载历史记录失败', 'error');
        } finally {
            this.isLoadingHistory = false;
            this.loadMoreBtn.classList.remove('group-chat__load-more--loading');
        }
    }

    handleHistoryLoaded(messages, hasMore) {
        this.hasMoreHistory = hasMore;
        this.loadedPageIndex++;
        this.loadedHistoryCount += messages.length;

        // 如果已加载超过1000条，隐藏加载更多按钮
        if (this.loadedHistoryCount >= this.maxHistoryCount) {
            this.hasMoreHistory = false;
            this.loadMoreBtn.style.display = 'none';
        } else if (!hasMore) {
            this.loadMoreBtn.style.display = 'none';
        } else {
            this.loadMoreBtn.style.display = 'block';
            this.loadMoreBtn.querySelector('span').textContent = '加载更多...';
        }

        // 插入到顶部（历史记录是倒序的，最新的在最后）
        const fragment = document.createDocumentFragment();
        messages.forEach(msg => {
            const isOwn = msg.userId === this.currentUserId;
            const messageEl = this.createMessageElement(msg, isOwn);
            fragment.insertBefore(messageEl, fragment.firstChild);
        });

        // 如果这是第一次加载，插入到容器顶部
        if (this.loadedPageIndex === 2) {
            this.messagesContainer.insertBefore(fragment, this.loadMoreBtn.nextSibling);
            // 滚动到底部
            this.scrollToBottom();
        } else {
            this.messagesContainer.insertBefore(fragment, this.loadMoreBtn.nextSibling);
        }
    }

    async sendMessage() {
        const message = this.input.value.trim();
        if (!message || !this.connection) return;

        // 禁用发送按钮
        this.sendBtn.disabled = true;
        this.input.disabled = true;

        try {
            await this.connection.invoke("SendMessage", message);
            this.input.value = '';
            this.input.style.height = 'auto';
        } catch (error) {
            outPutError(`发送消息失败: ${error.message || error}`);
            dialog.toast('发送失败，请重试', 'error');
        } finally {
            this.sendBtn.disabled = false;
            this.input.disabled = false;
            this.input.focus();
        }
    }

    addMessage(message, isOwn = false) {
        // 如果是自己的消息，更新currentUserId
        if (isOwn && !this.currentUserId) {
            this.currentUserId = message.userId;
        }

        const messageEl = this.createMessageElement(message, isOwn);
        this.messagesContainer.appendChild(messageEl);
        this.scrollToBottom();
    }

    createMessageElement(message, isOwn = false) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `group-chat__message${isOwn ? ' group-chat__message--own' : ''}`;

        const avatar = document.createElement('img');
        avatar.className = 'group-chat__avatar';
        avatar.src = message.avatar;
        avatar.alt = message.userName;

        const contentDiv = document.createElement('div');
        contentDiv.className = 'group-chat__message-content';

        const username = document.createElement('div');
        username.className = 'group-chat__username';
        username.textContent = message.userName;

        const bubble = document.createElement('div');
        bubble.className = 'group-chat__bubble';
        bubble.textContent = message.message;

        const timestamp = document.createElement('div');
        timestamp.className = 'group-chat__timestamp';
        timestamp.textContent = this.formatTime(message.sendTime);

        contentDiv.appendChild(username);
        contentDiv.appendChild(bubble);
        contentDiv.appendChild(timestamp);

        messageDiv.appendChild(avatar);
        messageDiv.appendChild(contentDiv);

        return messageDiv;
    }

    addSystemMessage(text) {
        const systemDiv = document.createElement('div');
        systemDiv.className = 'group-chat__system-message';
        systemDiv.textContent = text;
        this.messagesContainer.appendChild(systemDiv);
        this.scrollToBottom();
    }

    formatTime(dateTime) {
        const date = new Date(dateTime);
        const now = new Date();
        const diff = (now - date) / 1000; // 秒

        if (diff < 60) {
            return '刚刚';
        } else if (diff < 3600) {
            return `${Math.floor(diff / 60)}分钟前`;
        } else if (diff < 86400) {
            return `${Math.floor(diff / 3600)}小时前`;
        } else {
            return date.toLocaleDateString('zh-CN', {
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit'
            });
        }
    }

    scrollToBottom() {
        requestAnimationFrame(() => {
            this.messagesContainer.scrollTop = this.messagesContainer.scrollHeight;
        });
    }

    async close() {
        if (!this.isOpen) return;
        
        if (this.connection) {
            try {
                await this.connection.stop();
            } catch (error) {
                outPutError(`断开连接失败: ${error.message || error}`);
            }
        }

        if (this.chatPanel) {
            this.chatPanel.classList.remove('group-chat--visible');
            setTimeout(() => {
                if (this.chatPanel.parentNode) {
                    this.chatPanel.parentNode.removeChild(this.chatPanel);
                }
            }, 300);
        }

        this.isOpen = false;
        this.connection = null;
        this.currentUserId = null;
        this.loadedPageIndex = 0;
        this.hasMoreHistory = true;
        this.isLoadingHistory = false;
        this.loadedHistoryCount = 0;

        // 移动端：清除 hash 和恢复滚动
        if (this.isMobile()) {
            this.enableBodyScroll();
            // 清除 hash，但不添加历史记录
            if (window.location.hash === this.CHAT_HASH) {
                history.replaceState(null, '', window.location.pathname + window.location.search);
            }
        }

        // 显示触发按钮
        if (this.triggerButton) {
            this.triggerButton.style.display = 'flex';
        }
    }
}

// 导出单例
export const groupChat = new GroupChat();

