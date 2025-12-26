export class TimelineObserver {
    constructor(dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        this.observer = null;
    }

    init(elementsSelector) {
        const items = document.querySelectorAll(elementsSelector);
        if (items.length === 0) return;

        const observerOptions = {
            root: null,
            rootMargin: '0px',
            threshold: 0.1
        };

        this.observer = new IntersectionObserver((entries) => {
            const visibleIndices = [];
            
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const index = entry.target.getAttribute('data-index');
                    if (index !== null) {
                        visibleIndices.push(parseInt(index));
                        this.observer.unobserve(entry.target);
                    }
                }
            });

            if (visibleIndices.length > 0) {
                this.dotNetHelper.invokeMethodAsync('OnItemsVisible', visibleIndices);
            }
        }, observerOptions);

        items.forEach(item => {
            this.observer.observe(item);
        });
    }

    dispose() {
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }
        this.dotNetHelper = null;
    }
}

export function initTimeline(dotNetHelper, selector) {
    const timeline = new TimelineObserver(dotNetHelper);
    timeline.init(selector);
    return timeline;
}

