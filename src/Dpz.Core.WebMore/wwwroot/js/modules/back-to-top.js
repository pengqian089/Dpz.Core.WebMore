/**
 * Back To Top Component
 * Handles the visibility and scroll behavior of the back-to-top button
 */

export class BackToTop {
    constructor() {
        this.button = document.getElementById('back-to-top');
        this.threshold = 300; // Scroll threshold in pixels

        if (!this.button) return;

        this.init();
    }

    init() {
        this.bindEvents();
        this.checkScroll(); // Check initial state
    }

    bindEvents() {
        // Throttled scroll handler
        let ticking = false;
        window.addEventListener('scroll', () => {
            if (!ticking) {
                window.requestAnimationFrame(() => {
                    this.checkScroll();
                    ticking = false;
                });
                ticking = true;
            }
        });

        // Click handler
        this.button.addEventListener('click', (e) => {
            e.preventDefault();
            this.scrollToTop();
        });
    }

    checkScroll() {
        if (window.scrollY > this.threshold) {
            this.button.classList.add('is-visible');
        } else {
            this.button.classList.remove('is-visible');
        }
    }

    scrollToTop() {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    }
}


