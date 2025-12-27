export function show(trigger, content, placement) {
    if (!trigger || !content) return;

    // Must show first to calculate dimensions correctly
    try {
        content.showPopover();
    } catch (e) {
        // Fallback or ignore if already shown
        return;
    }

    const triggerRect = trigger.getBoundingClientRect();
    const contentRect = content.getBoundingClientRect();
    const gap = 8; // spacing

    let top = 0;
    let left = 0;
    let finalPlacement = placement;

    // Auto placement logic if placement is missing or empty
    if (!finalPlacement) {
        // Decide best placement based on available space
        const spaceTop = triggerRect.top;
        const spaceBottom = window.innerHeight - triggerRect.bottom;
        const spaceLeft = triggerRect.left;
        const spaceRight = window.innerWidth - triggerRect.right;

        // Simple heuristic: choose the side with the most space
        // Or prefer Top/Bottom over Left/Right
        if (spaceTop >= contentRect.height + gap) {
            finalPlacement = 'Top';
        } else if (spaceBottom >= contentRect.height + gap) {
            finalPlacement = 'Bottom';
        } else if (spaceRight >= contentRect.width + gap) {
            finalPlacement = 'Right';
        } else {
            finalPlacement = 'Left'; // Default fallback
        }
    }

    // --- Calculation Logic ---

    // Initial calculation based on placement
    switch (finalPlacement) {
        case 'Top':
            top = triggerRect.top - contentRect.height - gap;
            left = triggerRect.left + (triggerRect.width - contentRect.width) / 2;
            break;
        case 'Bottom':
            top = triggerRect.bottom + gap;
            left = triggerRect.left + (triggerRect.width - contentRect.width) / 2;
            break;
        case 'Left':
            top = triggerRect.top + (triggerRect.height - contentRect.height) / 2;
            left = triggerRect.left - contentRect.width - gap;
            break;
        case 'Right':
            top = triggerRect.top + (triggerRect.height - contentRect.height) / 2;
            left = triggerRect.right + gap;
            break;
        default: // Default to Top if something weird happens
            top = triggerRect.top - contentRect.height - gap;
            left = triggerRect.left + (triggerRect.width - contentRect.width) / 2;
            break;
    }

    // --- Boundary Correction (Viewport Awareness) ---

    // 1. Horizontal Boundary (Prevent horizontal scrollbar)
    const viewportWidth = window.innerWidth;
    const padding = 8; // Minimum distance from screen edge

    if (left < padding) {
        left = padding;
    } else if (left + contentRect.width > viewportWidth - padding) {
        left = viewportWidth - contentRect.width - padding;
    }

    // 2. Vertical Boundary
    const viewportHeight = window.innerHeight;
    
    // If it goes off the top
    if (top < padding) {
        top = padding;
    } 
    // If it goes off the bottom is less critical for scrollbar usually, but good for visibility
    // If it goes really far down, maybe we should have flipped it? (Not implementing flip here for manual placement, just clamping)
    else if (top + contentRect.height > viewportHeight - padding) {
        // If it's forced off bottom, maybe just clamp it or let it scroll vertically if main page scrolls.
        // For popover, clamping is usually safer to keep it visible.
        // top = viewportHeight - contentRect.height - padding; 
        // NOTE: Clamping top might cover the trigger element. 
        // Ideally, we flip placement, but for "fixed" placement we just ensure visibility.
    }

    // Update CSS classes for arrow
    // Remove all old placement classes first
    content.classList.remove('tooltip__content--top', 'tooltip__content--bottom', 'tooltip__content--left', 'tooltip__content--right');
    // Add the computed placement class
    content.classList.add(`tooltip__content--${finalPlacement.toLowerCase()}`);

    // Apply calculated coordinates
    content.style.top = `${top}px`;
    content.style.left = `${left}px`;
    content.style.margin = '0'; // Ensure no browser default margins
}

export function hide(content) {
    if (!content) return;
    try {
        content.hidePopover();
    } catch (e) {
        // Ignore if already hidden
    }
}
