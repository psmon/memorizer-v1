/**
 * Markdown Renderer with Mermaid Support
 * Common utility for rendering markdown with code highlighting and mermaid diagrams
 */

// Utility function to escape HTML
function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

// Language display name mappings
const LANGUAGE_NAMES = {
    'js': 'JavaScript',
    'javascript': 'JavaScript',
    'ts': 'TypeScript',
    'typescript': 'TypeScript',
    'py': 'Python',
    'python': 'Python',
    'cs': 'C#',
    'csharp': 'C#',
    'cpp': 'C++',
    'c++': 'C++',
    'c': 'C',
    'java': 'Java',
    'html': 'HTML',
    'css': 'CSS',
    'scss': 'SCSS',
    'sass': 'Sass',
    'less': 'Less',
    'sql': 'SQL',
    'json': 'JSON',
    'xml': 'XML',
    'yaml': 'YAML',
    'yml': 'YAML',
    'bash': 'Bash',
    'shell': 'Shell',
    'sh': 'Shell',
    'powershell': 'PowerShell',
    'ps1': 'PowerShell',
    'dockerfile': 'Dockerfile',
    'docker': 'Docker',
    'md': 'Markdown',
    'markdown': 'Markdown',
    'plaintext': 'Plain Text',
    'text': 'Plain Text',
    'go': 'Go',
    'rust': 'Rust',
    'ruby': 'Ruby',
    'rb': 'Ruby',
    'php': 'PHP',
    'swift': 'Swift',
    'kotlin': 'Kotlin',
    'r': 'R',
    'scala': 'Scala',
    'lua': 'Lua',
    'perl': 'Perl',
    'vim': 'Vim Script',
    'nginx': 'Nginx',
    'apache': 'Apache',
    'ini': 'INI',
    'toml': 'TOML',
    'jsx': 'JSX',
    'tsx': 'TSX',
    'vue': 'Vue',
    'svelte': 'Svelte',
    'mermaid': 'Mermaid'
};

/**
 * Initialize mermaid with default configuration
 */
function initializeMermaid() {
    if (typeof mermaid !== 'undefined' && !window.mermaidInitialized) {
        mermaid.initialize({
            startOnLoad: false,
            theme: 'default',
            themeVariables: {
                primaryColor: '#667eea',
                primaryTextColor: '#fff',
                primaryBorderColor: '#764ba2',
                lineColor: '#5a67d8',
                secondaryColor: '#f8f9fa',
                tertiaryColor: '#e9ecef',
                background: '#ffffff',
                mainBkg: '#667eea',
                secondBkg: '#f8f9fa',
                tertiaryBkg: '#e9ecef',
                fontSize: '16px'
            },
            flowchart: {
                useMaxWidth: true,
                htmlLabels: true,
                curve: 'basis',
                width: '100%'
            },
            securityLevel: 'loose',
            logLevel: 'error'
        });
        window.mermaidInitialized = true;
    }
}

/**
 * Configure marked options with syntax highlighting
 */
function configureMarked() {
    if (typeof marked !== 'undefined') {
        marked.setOptions({
            highlight: function(code, lang) {
                // Skip mermaid blocks from syntax highlighting
                if (lang && lang.toLowerCase() === 'mermaid') {
                    return code;
                }
                
                if (typeof hljs !== 'undefined') {
                    const language = lang || 'plaintext';
                    if (language && hljs.getLanguage(language)) {
                        try {
                            return hljs.highlight(code, { language: language }).value;
                        } catch (e) {
                            console.error('Highlight error:', e);
                        }
                    }
                    // Auto-detect language if not specified or not found
                    try {
                        const result = hljs.highlightAuto(code);
                        return result.value;
                    } catch (e) {
                        console.error('Auto-highlight error:', e);
                    }
                }
                return code;
            },
            breaks: true,
            gfm: true,
            langPrefix: 'language-'
        });
    }
}

/**
 * Create code block toolbar with language label and copy button
 */
function createCodeBlockToolbar(pre, language) {
    // Skip if toolbar already exists
    if (pre.querySelector('.code-block-toolbar')) return;
    
    // Create wrapper div
    const wrapper = document.createElement('div');
    wrapper.className = 'code-block-wrapper';
    
    // Insert wrapper and move pre into it
    if (pre.parentNode) {
        pre.parentNode.insertBefore(wrapper, pre);
        wrapper.appendChild(pre);
    }
    
    // Format language display name
    const displayLanguage = LANGUAGE_NAMES[language?.toLowerCase()] || 
                           (language ? language.charAt(0).toUpperCase() + language.slice(1) : 'Plain Text');
    
    // Create toolbar div
    const toolbar = document.createElement('div');
    toolbar.className = 'code-block-toolbar';
    
    // Create language label
    const langLabel = document.createElement('span');
    langLabel.className = 'code-language-label';
    langLabel.innerHTML = `<i class="fas fa-code"></i> ${displayLanguage}`;
    
    // Create copy button
    const button = document.createElement('button');
    button.className = 'copy-code-button';
    button.innerHTML = '<i class="fas fa-copy"></i> Copy';
    button.onclick = function() {
        const code = pre.querySelector('code');
        const text = code ? code.textContent : pre.textContent;
        
        navigator.clipboard.writeText(text).then(() => {
            button.innerHTML = '<i class="fas fa-check"></i> Copied!';
            button.classList.add('copied');
            setTimeout(() => {
                button.innerHTML = '<i class="fas fa-copy"></i> Copy';
                button.classList.remove('copied');
            }, 2000);
        }).catch(err => {
            console.error('Failed to copy:', err);
            button.innerHTML = '<i class="fas fa-times"></i> Failed';
            setTimeout(() => {
                button.innerHTML = '<i class="fas fa-copy"></i> Copy';
            }, 2000);
        });
    };
    
    toolbar.appendChild(langLabel);
    toolbar.appendChild(button);
    wrapper.insertBefore(toolbar, pre);
}

/**
 * Process code blocks - add toolbars and detect language
 */
function processCodeBlocks(container) {
    const codeBlocks = container.querySelectorAll('pre');
    
    codeBlocks.forEach(pre => {
        // Skip if already processed
        if (pre.closest('.code-block-wrapper')) return;
        
        // Detect language from code block
        const codeElement = pre.querySelector('code');
        let language = 'plaintext';
        
        if (codeElement) {
            // Check for language class
            const classList = codeElement.className.split(' ');
            const langClass = classList.find(cls => cls.startsWith('language-'));
            
            if (langClass) {
                language = langClass.replace('language-', '');
            } else if (typeof hljs !== 'undefined' && !codeElement.className.includes('hljs')) {
                // Try to detect and highlight if not already highlighted
                try {
                    const result = hljs.highlightAuto(codeElement.textContent);
                    if (result.language) {
                        language = result.language;
                        codeElement.innerHTML = result.value;
                        codeElement.className = `language-${language} hljs`;
                    }
                } catch (e) {
                    console.error('Language detection error:', e);
                }
            }
        }
        
        // Don't add toolbar to mermaid blocks (they'll be processed separately)
        if (language.toLowerCase() !== 'mermaid') {
            createCodeBlockToolbar(pre, language);
        }
    });
}

/**
 * Create control buttons for mermaid diagram zoom/pan
 */
function createMermaidControls(panZoomInstance) {
    const controlsDiv = document.createElement('div');
    controlsDiv.className = 'mermaid-controls';
    
    // Zoom In button
    const zoomInBtn = document.createElement('button');
    zoomInBtn.className = 'mermaid-control-btn';
    zoomInBtn.innerHTML = '<i class="fas fa-search-plus"></i>';
    zoomInBtn.title = 'Zoom In';
    zoomInBtn.onclick = () => panZoomInstance.zoomIn();
    
    // Zoom Out button
    const zoomOutBtn = document.createElement('button');
    zoomOutBtn.className = 'mermaid-control-btn';
    zoomOutBtn.innerHTML = '<i class="fas fa-search-minus"></i>';
    zoomOutBtn.title = 'Zoom Out';
    zoomOutBtn.onclick = () => panZoomInstance.zoomOut();
    
    // Reset button
    const resetBtn = document.createElement('button');
    resetBtn.className = 'mermaid-control-btn';
    resetBtn.innerHTML = '<i class="fas fa-compress"></i>';
    resetBtn.title = 'Reset View';
    resetBtn.onclick = () => panZoomInstance.resetZoom();
    
    // Fit button
    const fitBtn = document.createElement('button');
    fitBtn.className = 'mermaid-control-btn';
    fitBtn.innerHTML = '<i class="fas fa-expand"></i>';
    fitBtn.title = 'Fit to Screen';
    fitBtn.onclick = () => {
        panZoomInstance.fit();
        panZoomInstance.center();
    };
    
    controlsDiv.appendChild(zoomInBtn);
    controlsDiv.appendChild(zoomOutBtn);
    controlsDiv.appendChild(resetBtn);
    controlsDiv.appendChild(fitBtn);
    
    return controlsDiv;
}

/**
 * Render mermaid diagrams in the container
 */
async function renderMermaidDiagrams(container) {
    if (typeof mermaid === 'undefined') {
        console.warn('Mermaid library not loaded');
        return;
    }
    
    // Find all mermaid code blocks
    const mermaidBlocks = container.querySelectorAll('pre > code.language-mermaid');
    
    if (mermaidBlocks.length === 0) {
        return;
    }
    
    console.log(`Found ${mermaidBlocks.length} mermaid blocks to render`);
    
    // Process each mermaid block
    for (let i = 0; i < mermaidBlocks.length; i++) {
        const codeBlock = mermaidBlocks[i];
        const pre = codeBlock.parentElement;
        const mermaidCode = codeBlock.textContent.trim();
        
        console.log(`Processing mermaid block ${i + 1}:`, mermaidCode.substring(0, 50));
        
        // Create unique ID - ensure truly unique
        const timestamp = Date.now();
        const random = Math.random().toString(36).substring(2, 15);
        const mermaidId = `mermaid-${timestamp}-${random}-${i}`;
        
        // Create wrapper elements
        const mermaidWrapper = document.createElement('div');
        mermaidWrapper.className = 'mermaid-wrapper';
        
        // Replace the pre element with wrapper first
        if (pre.parentNode) {
            const wrapper = pre.closest('.code-block-wrapper');
            if (wrapper && wrapper.parentNode) {
                wrapper.parentNode.replaceChild(mermaidWrapper, wrapper);
            } else if (pre.parentNode) {
                pre.parentNode.replaceChild(mermaidWrapper, pre);
            }
        }
        
        try {
            // Create a container for mermaid
            const mermaidContainer = document.createElement('div');
            mermaidContainer.className = 'mermaid';
            mermaidContainer.textContent = mermaidCode;
            
            // Add to wrapper first (must be in DOM for mermaid to work)
            mermaidWrapper.appendChild(mermaidContainer);
            
            // Initialize mermaid if needed
            if (!window.mermaidInitialized) {
                mermaid.initialize({
                    startOnLoad: false,
                    theme: 'default',
                    flowchart: {
                        useMaxWidth: true,
                        htmlLabels: true,
                        width: '100%'
                    },
                    themeVariables: {
                        fontSize: '16px'
                    },
                    securityLevel: 'loose'
                });
                window.mermaidInitialized = true;
            }
            
            // Render using mermaid.run which processes elements with class="mermaid"
            await mermaid.run({
                querySelector: '.mermaid',
                suppressErrors: false,
                nodes: [mermaidContainer]
            });
            
            console.log(`Successfully rendered mermaid block ${i + 1}`);
            
            // Add zoom and pan controls if svg-pan-zoom is available
            if (typeof svgPanZoom !== 'undefined') {
                const svgElement = mermaidContainer.querySelector('svg');
                if (svgElement) {
                    // Make wrapper position relative for absolute positioned controls
                    mermaidWrapper.style.position = 'relative';
                    
                    // Get parent container dimensions
                    const wrapperWidth = mermaidWrapper.clientWidth - 32; // Subtract padding
                    const wrapperHeight = Math.max(600, svgElement.getBoundingClientRect().height); // Min 600px height
                    
                    // Set SVG to use full parent width
                    svgElement.setAttribute('width', '100%');
                    svgElement.setAttribute('height', wrapperHeight);
                    svgElement.style.maxWidth = '100%';
                    svgElement.style.width = '100%';
                    svgElement.style.height = wrapperHeight + 'px';
                    
                    // Get the actual viewBox or set one if missing
                    if (!svgElement.hasAttribute('viewBox')) {
                        const bbox = svgElement.getBBox();
                        svgElement.setAttribute('viewBox', `0 0 ${bbox.width} ${bbox.height}`);
                    }
                    
                    // Initialize svg-pan-zoom with proper sizing
                    const panZoomInstance = svgPanZoom(svgElement, {
                        zoomEnabled: true,
                        controlIconsEnabled: false, // We'll use custom controls
                        fit: true,
                        center: true,
                        minZoom: 0.3,
                        maxZoom: 10,
                        zoomScaleSensitivity: 0.2,
                        dblClickZoomEnabled: true,
                        mouseWheelZoomEnabled: true,
                        contain: false,
                        viewportSelector: '.svg-pan-zoom_viewport'
                    });
                    
                    // Ensure proper sizing after initialization
                    setTimeout(() => {
                        panZoomInstance.resize();
                        panZoomInstance.fit();
                        panZoomInstance.center();
                    }, 100);
                    
                    // Create control buttons
                    const controls = createMermaidControls(panZoomInstance);
                    mermaidWrapper.appendChild(controls);
                    
                    // Store instance for cleanup if needed
                    mermaidWrapper.panZoomInstance = panZoomInstance;
                }
            }
        } catch (error) {
            console.error('Mermaid rendering failed:', error);
            // Show error with original code
            mermaidWrapper.className = 'mermaid-error';
            mermaidWrapper.innerHTML = `
                <div class="alert alert-warning">
                    <i class="fas fa-exclamation-triangle"></i> Mermaid Diagram Error
                    <pre>${escapeHtml(error.message || 'Failed to render diagram')}</pre>
                    <details>
                        <summary>Original Mermaid Code</summary>
                        <pre>${escapeHtml(mermaidCode)}</pre>
                    </details>
                </div>
            `;
        }
    }
    
    console.log('Completed mermaid rendering');
}

/**
 * Main render function - renders markdown with all enhancements
 */
async function renderMarkdownContent(markdownText, targetElement) {
    if (!markdownText || !targetElement) {
        console.error('Missing markdown text or target element');
        return;
    }
    
    // Configure marked if not already done
    configureMarked();
    
    // Initialize mermaid if available
    if (typeof mermaid !== 'undefined') {
        initializeMermaid();
    }
    
    // Render markdown to HTML
    let html = '';
    if (typeof marked !== 'undefined') {
        try {
            html = marked.parse(markdownText);
        } catch (e) {
            console.error('Markdown parsing error:', e);
            html = escapeHtml(markdownText).replace(/\n/g, '<br>');
        }
    } else {
        console.warn('Marked library not loaded, displaying raw text');
        html = escapeHtml(markdownText).replace(/\n/g, '<br>');
    }
    
    // Set the HTML content
    targetElement.innerHTML = html;
    
    // Add markdown-content class if not present
    if (!targetElement.classList.contains('markdown-content')) {
        targetElement.classList.add('markdown-content');
    }
    
    // Process code blocks (add toolbars)
    processCodeBlocks(targetElement);
    
    // Render mermaid diagrams
    await renderMermaidDiagrams(targetElement);
    
    // Apply syntax highlighting if needed
    if (typeof hljs !== 'undefined') {
        targetElement.querySelectorAll('pre code:not(.hljs)').forEach((block) => {
            // Skip mermaid blocks
            if (!block.classList.contains('language-mermaid')) {
                hljs.highlightElement(block);
            }
        });
    }
}

/**
 * Quick render for preview (without full processing)
 */
function renderMarkdownPreview(markdownText, maxChars = 300) {
    if (!markdownText) return '';
    
    // Truncate if needed
    let preview = markdownText.length > maxChars 
        ? markdownText.substring(0, maxChars) + '...' 
        : markdownText;
    
    // Basic markdown to HTML
    if (typeof marked !== 'undefined') {
        try {
            return marked.parse(preview);
        } catch (e) {
            console.error('Preview parsing error:', e);
        }
    }
    
    return escapeHtml(preview).replace(/\n/g, '<br>');
}

// Initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        configureMarked();
        if (typeof mermaid !== 'undefined') {
            initializeMermaid();
        }
    });
} else {
    configureMarked();
    if (typeof mermaid !== 'undefined') {
        initializeMermaid();
    }
}