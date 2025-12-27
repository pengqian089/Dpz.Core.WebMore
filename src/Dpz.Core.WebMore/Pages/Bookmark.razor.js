export function init(selector) {
    const grid = document.querySelector(selector);
    if (!grid) return;

    const layout = () => {
        const items = Array.from(grid.children);
        if (items.length === 0) return;

        const gridStyle = window.getComputedStyle(grid);
        const gap = parseInt(gridStyle.getPropertyValue('gap') || gridStyle.getPropertyValue('grid-gap') || '0');
        const rowHeight = 1; 

        items.forEach(item => {
             item.style.height = 'auto';
             item.style.gridRowEnd = 'auto';
             
             const contentHeight = item.getBoundingClientRect().height;
             const rowSpan = Math.ceil((contentHeight + gap) / (rowHeight + gap));
             
             item.style.gridRowEnd = `span ${rowSpan}`;
        });
    };

    let layoutTimer;
    const debouncedLayout = () => {
        if (layoutTimer) clearTimeout(layoutTimer);
        layoutTimer = setTimeout(() => {
            requestAnimationFrame(layout);
        }, 50);
    };

    // Initial layout
    debouncedLayout();

    // Image loading for existing images
    const setupImageListeners = (parent) => {
        const images = parent.querySelectorAll('img');
        images.forEach(img => {
            if (!img.complete) {
                img.addEventListener('load', debouncedLayout);
                img.addEventListener('error', debouncedLayout);
            }
        });
    };
    setupImageListeners(grid);

    // ResizeObserver
    let resizeObserver;
    if ('ResizeObserver' in window) {
        resizeObserver = new ResizeObserver(() => {
            debouncedLayout();
        });
        resizeObserver.observe(grid);
        Array.from(grid.children).forEach(child => resizeObserver.observe(child));
    }

    // MutationObserver to handle new items
    const mutationObserver = new MutationObserver((mutations) => {
        let shouldLayout = false;
        mutations.forEach(m => {
             if (m.addedNodes.length > 0) {
                 shouldLayout = true;
                 m.addedNodes.forEach(node => {
                     if (node instanceof Element) {
                         setupImageListeners(node);
                         if (resizeObserver) resizeObserver.observe(node);
                     }
                 });
             }
             if (m.removedNodes.length > 0) {
                 shouldLayout = true;
             }
        });
        if (shouldLayout) debouncedLayout();
    });
    mutationObserver.observe(grid, { childList: true });

    return {
        dispose: () => {
            mutationObserver.disconnect();
            if (resizeObserver) resizeObserver.disconnect();
        },
        layout: debouncedLayout
    };
}

export function scrollToItem(index) {
    const list = document.querySelector('.bookmark__search-list');
    if (!list) return;
    const items = list.querySelectorAll('.bookmark__search-item');
    if (items[index]) {
        items[index].scrollIntoView({ block: 'nearest' });
    }
}
