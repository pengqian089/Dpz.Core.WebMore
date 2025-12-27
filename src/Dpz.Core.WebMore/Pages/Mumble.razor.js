export class MumbleObserver {
    constructor() {
        this.observer = null;
        this.dotNetRef = null;
        this.observedIds = new Set();
        this.stateMap = new Map();
    }

    init(dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.observer = new ResizeObserver(entries => {
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
                    // Use try-catch to avoid unhandled promise rejections if component disposed
                    try {
                        this.dotNetRef.invokeMethodAsync('UpdateExpandState', id, needExpand);
                    } catch (e) {
                        console.debug("Failed to invoke UpdateExpandState", e);
                    }
                }
            }
        });
    }

    observe(element, id) {
        if (!this.observer || !element) return;
        
        element.setAttribute('data-mumble-id', id);
        this.observer.observe(element);
        this.observedIds.add(id);
    }

    dispose() {
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }
        this.dotNetRef = null;
        this.observedIds.clear();
        this.stateMap.clear();
    }
}

export function createObserver() {
    return new MumbleObserver();
}
