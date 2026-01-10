/**
 * GroupChat 隔离 JavaScript 模块
 */

export function isMobile(breakpoint) {
    return window.innerWidth <= breakpoint;
}

export function addHash(hash) {
    if (window.location.hash !== hash) {
        history.pushState(null, '', hash);
    }
}

export function removeHash(hash) {
    if (window.location.hash === hash) {
        history.back();
    }
}

export function disableBodyScroll() {
    if (!document.body.hasAttribute('data-scroll-locked')) {
        const scrollY = window.scrollY;
        document.body.setAttribute('data-scroll-locked', 'true');
        document.body.setAttribute('data-scroll-y', scrollY.toString());
        document.body.style.position = 'fixed';
        document.body.style.top = `-${scrollY}px`;
        document.body.style.width = '100%';
        document.body.style.overflow = 'hidden';
    }
}

export function enableBodyScroll() {
    if (document.body.hasAttribute('data-scroll-locked')) {
        const scrollY = document.body.getAttribute('data-scroll-y');
        document.body.removeAttribute('data-scroll-locked');
        document.body.removeAttribute('data-scroll-y');
        document.body.style.position = '';
        document.body.style.top = '';
        document.body.style.width = '';
        document.body.style.overflow = '';
        window.scrollTo(0, parseInt(scrollY || '0'));
    }
}

export function scrollToBottom() {
    const el = document.querySelector('.group-chat__messages');
    if (el) {
        setTimeout(() => {
            el.scrollTop = el.scrollHeight;
        }, 0);
    }
}

export function preventEnterKey() {
    const textarea = document.querySelector('.group-chat__input');
    if (textarea) {
        textarea.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
            }
        });
    }
}
