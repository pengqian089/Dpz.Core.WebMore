export class CodeArea{
    
    /**
     * CodeArea
     * @param {HTMLElement} element
     * */
    constructor(element= null) {
        this.element = element || document;
        this.index = 0;
        this.init();
        
    }
    
    init() {
        if (typeof Prism !== 'undefined') {
            const prismTheme = document.getElementById('prism_theme');
            if (!prismTheme?.href) {
                if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
                    prismTheme.href = 'https://dpangzi.com/library/prism/prism-tomorrow-night.css';
                } else {
                    prismTheme.href = 'https://dpangzi.com/library/prism/prism-light.css';
                }
            }
            const that = this;
            this.element.querySelectorAll('pre code').forEach((block, index) => {
                // Prevent infinite loop: check if already highlighted
                if (block.getAttribute('data-highlighted') === 'true') {
                    return;
                }

                const pre = block.parentNode;
                if (!pre.id) {
                    pre.id = `code-block-article-${index}`;
                }
                if (!pre.classList.contains('rainbow-braces')) {
                    pre.classList.add('rainbow-braces');
                }
                if (!pre.classList.contains('match-braces')) {
                    pre.classList.add('match-braces');
                }
                if (!pre.classList.contains('line-numbers')) {
                    pre.classList.add('line-numbers');
                }
                if (!pre.classList.contains('linkable-line-numbers')) {
                    pre.classList.add('linkable-line-numbers');
                }
                if (!pre.hasAttribute('data-line')) {
                    pre.setAttribute('data-line', '');
                }
                that.index++;
                console.log(that.index);
                Prism.highlightElement(block);
                block.setAttribute('data-highlighted', 'true');
            });

        }
    }
}