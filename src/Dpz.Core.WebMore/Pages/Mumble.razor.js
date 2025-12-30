export class MumbleObserver {
    constructor() {
        this.observer = null;
        this.dotNetRef = null;
        this.stateMap = new Map();
    }

    init(dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.observer = new ResizeObserver(entries => {
            // Skip if DOM processing is paused (e.g., dialog is open)
            if (window.appDOMManager && window.appDOMManager.paused) {
                return;
            }

            for (const entry of entries) {
                const target = entry.target;
                const id = target.getAttribute('data-mumble-id');
                if (!id) continue;

                const height = target.scrollHeight;
                const needExpand = height > 300;

                const lastState = this.stateMap.get(id);
                // Trigger if state changed or if it's the first time (undefined)
                if (lastState !== needExpand) {
                    this.stateMap.set(id, needExpand);
                    try {
                        this.dotNetRef.invokeMethodAsync('UpdateExpandState', id, needExpand);
                    } catch (e) {
                        // Silently ignore if component disposed
                    }
                }
            }
        });
    }

    observe(element, id) {
        if (!this.observer || !element) return;
        
        element.setAttribute('data-mumble-id', id);
        this.observer.observe(element);
    }

    dispose() {
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }
        this.dotNetRef = null;
        this.stateMap.clear();
    }
}

export function createObserver() {
    return new MumbleObserver();
}
