export function initResizableSidebar(resizerElement, sidebarElement) {
    if (!resizerElement || !sidebarElement) return;

    let isResizing = false;
    let startX = 0;
    let startWidth = 0;

    resizerElement.addEventListener('mousedown', (e) => {
        isResizing = true;
        startX = e.clientX;
        startWidth = parseInt(getComputedStyle(sidebarElement).width, 10);
        
        document.body.style.cursor = 'col-resize';
        resizerElement.classList.add('is-resizing');
        e.preventDefault();
    });

    document.addEventListener('mousemove', (e) => {
        if (!isResizing) return;

        const diffX = e.clientX - startX;
        const newWidth = Math.max(200, Math.min(600, startWidth + diffX));
        
        document.documentElement.style.setProperty('--code-sidebar-width', `${newWidth}px`);
    });

    document.addEventListener('mouseup', () => {
        if (isResizing) {
            isResizing = false;
            document.body.style.cursor = '';
            resizerElement.classList.remove('is-resizing');
        }
    });
}

export function scrollToActive(container) {
    if (!container) return;
    setTimeout(() => {
        const active = container.querySelector('.code-tree__content.is-active');
        if (active) {
            active.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }, 100);
}
