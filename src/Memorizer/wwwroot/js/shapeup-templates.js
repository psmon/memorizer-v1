/**
 * ShapeUp Whiteboard Templates and AI Generation
 * Board templates and AI-based board generation functions
 */

// ============================================================================
// Board Templates
// ============================================================================
function addBoardTemplate(type) {
    // Guard: ensure canvas is initialized
    if (!canvas) {
        console.error('Canvas not initialized');
        return;
    }

    const colors = {
        'problem': '#667eea',
        'breadboard': '#00c6ff',
        'fat-marker': '#11998e',
        'risk': '#f093fb',
        'pitch': '#ff6b6b'
    };

    const color = colors[type] || '#667eea';
    const centerX = (canvas.width || 800) / 2 - 200;
    const centerY = 100;

    switch (type) {
        case 'problem':
            addProblemBoard(centerX, centerY, color);
            break;
        case 'breadboard':
            addBreadboard(centerX, centerY, color);
            break;
        case 'fat-marker':
            addFatMarkerSketch(centerX, centerY, color);
            break;
        case 'risk':
            addRiskBoard(centerX, centerY, color);
            break;
        case 'pitch':
            addPitchBoard(centerX, centerY, color);
            break;
    }
}

function addProblemBoard(x, y, color) {
    const frame = new fabric.Rect({
        left: x, top: y,
        width: 500, height: 350,
        fill: 'white',
        stroke: color,
        strokeWidth: 3,
        rx: 12, ry: 12
    });

    const title = new fabric.IText('PROBLEM DEFINITION', {
        left: x + 20, top: y + 15,
        fontSize: 16, fontWeight: 'bold',
        fill: color
    });

    // 4 sections
    const sections = [
        { x: x + 10, y: y + 50, w: 235, h: 130, label: 'Raw Idea', content: '[Original idea]' },
        { x: x + 255, y: y + 50, w: 235, h: 130, label: 'Narrowed Problem', content: '[Specific problem]' },
        { x: x + 10, y: y + 190, w: 235, h: 130, label: 'Baseline', content: '[Current state]' },
        { x: x + 255, y: y + 190, w: 235, h: 130, label: 'Appetite', content: '[ ] Small (1-2w)\n[x] Big (6w)' }
    ];

    const objects = [frame, title];

    sections.forEach(s => {
        const box = new fabric.Rect({
            left: s.x, top: s.y,
            width: s.w, height: s.h,
            fill: '#f8f9fa',
            stroke: '#dee2e6',
            strokeWidth: 1,
            rx: 6, ry: 6
        });
        const label = new fabric.IText(s.label, {
            left: s.x + 10, top: s.y + 10,
            fontSize: 12, fontWeight: 'bold',
            fill: '#6c757d'
        });
        const content = new fabric.IText(s.content, {
            left: s.x + 10, top: s.y + 30,
            fontSize: 11,
            fill: '#495057'
        });
        objects.push(box, label, content);
    });

    const group = new fabric.Group(objects, { selectable: true });
    canvas.add(group);
    canvas.renderAll();
}

function addBreadboard(x, y, color) {
    const frame = new fabric.Rect({
        left: x, top: y,
        width: 600, height: 200,
        fill: 'white',
        stroke: color,
        strokeWidth: 3,
        rx: 12, ry: 12
    });

    const title = new fabric.IText('BREADBOARD', {
        left: x + 20, top: y + 15,
        fontSize: 16, fontWeight: 'bold',
        fill: color
    });

    const places = [
        { x: x + 20, y: y + 50, name: 'Screen 1', items: ['Button', 'Field'] },
        { x: x + 200, y: y + 50, name: 'Screen 2', items: ['Form', 'Submit'] },
        { x: x + 380, y: y + 50, name: 'Screen 3', items: ['Result', 'Done'] }
    ];

    const objects = [frame, title];

    places.forEach((p, i) => {
        const box = new fabric.Rect({
            left: p.x, top: p.y,
            width: 150, height: 120,
            fill: '#f8f9fa',
            stroke: '#333',
            strokeWidth: 2,
            rx: 6, ry: 6
        });
        const name = new fabric.IText(p.name, {
            left: p.x + 10, top: p.y + 10,
            fontSize: 14, fontWeight: 'bold',
            fill: '#333',
            underline: true
        });
        objects.push(box, name);

        p.items.forEach((item, j) => {
            const itemText = new fabric.IText('• ' + item, {
                left: p.x + 15, top: p.y + 40 + j * 20,
                fontSize: 12,
                fill: '#495057'
            });
            objects.push(itemText);
        });

        // Add arrow to next place
        if (i < places.length - 1) {
            const arrow = new fabric.Line([p.x + 155, p.y + 60, p.x + 195, p.y + 60], {
                stroke: '#333',
                strokeWidth: 2
            });
            const arrowHead = new fabric.Triangle({
                left: p.x + 195, top: p.y + 60,
                width: 10, height: 10,
                fill: '#333',
                angle: 90,
                originX: 'center', originY: 'center'
            });
            objects.push(arrow, arrowHead);
        }
    });

    const group = new fabric.Group(objects, { selectable: true });
    canvas.add(group);
    canvas.renderAll();
}

function addFatMarkerSketch(x, y, color) {
    const frame = new fabric.Rect({
        left: x, top: y,
        width: 350, height: 400,
        fill: 'white',
        stroke: color,
        strokeWidth: 3,
        rx: 12, ry: 12
    });

    const title = new fabric.IText('FAT MARKER SKETCH', {
        left: x + 20, top: y + 15,
        fontSize: 16, fontWeight: 'bold',
        fill: color
    });

    const boxes = [
        { x: x + 20, y: y + 50, w: 310, h: 40, label: 'Header' },
        { x: x + 20, y: y + 110, w: 145, h: 120, label: 'Main' },
        { x: x + 185, y: y + 110, w: 145, h: 120, label: 'Side' },
        { x: x + 20, y: y + 250, w: 310, h: 60, label: 'Actions' },
        { x: x + 20, y: y + 330, w: 310, h: 40, label: 'Footer' }
    ];

    const objects = [frame, title];

    boxes.forEach(b => {
        const box = new fabric.Rect({
            left: b.x, top: b.y,
            width: b.w, height: b.h,
            fill: 'transparent',
            stroke: '#333',
            strokeWidth: 4,
            rx: 4, ry: 4
        });
        const label = new fabric.IText(b.label, {
            left: b.x + 10, top: b.y + 10,
            fontSize: 12, fontWeight: 'bold',
            fill: '#6c757d'
        });
        objects.push(box, label);
    });

    const group = new fabric.Group(objects, { selectable: true });
    canvas.add(group);
    canvas.renderAll();
}

function addRiskBoard(x, y, color) {
    const frame = new fabric.Rect({
        left: x, top: y,
        width: 500, height: 350,
        fill: 'white',
        stroke: color,
        strokeWidth: 3,
        rx: 12, ry: 12
    });

    const title = new fabric.IText('RISK & RABBIT HOLES', {
        left: x + 20, top: y + 15,
        fontSize: 16, fontWeight: 'bold',
        fill: color
    });

    const rabbitTitle = new fabric.IText('Rabbit Holes:', {
        left: x + 20, top: y + 50,
        fontSize: 14, fontWeight: 'bold',
        fill: '#333'
    });

    const rabbitHoles = [
        '[Risk 1] - patched: [solution]',
        '[Risk 2] - out-of-bounds',
        '[Risk 3] - cut-back'
    ];

    const noGoTitle = new fabric.IText('No-Gos:', {
        left: x + 20, top: y + 180,
        fontSize: 14, fontWeight: 'bold',
        fill: '#333'
    });

    const noGos = [
        'Feature A',
        'Feature B',
        'Feature C'
    ];

    const objects = [frame, title, rabbitTitle, noGoTitle];

    rabbitHoles.forEach((rh, i) => {
        const text = new fabric.IText('• ' + rh, {
            left: x + 30, top: y + 80 + i * 25,
            fontSize: 12,
            fill: '#495057'
        });
        objects.push(text);
    });

    noGos.forEach((ng, i) => {
        const text = new fabric.IText('✗ ' + ng, {
            left: x + 30, top: y + 210 + i * 25,
            fontSize: 12,
            fill: '#dc3545'
        });
        objects.push(text);
    });

    const group = new fabric.Group(objects, { selectable: true });
    canvas.add(group);
    canvas.renderAll();
}

function addPitchBoard(x, y, color) {
    const frame = new fabric.Rect({
        left: x, top: y,
        width: 700, height: 500,
        fill: 'white',
        stroke: color,
        strokeWidth: 3,
        rx: 12, ry: 12
    });

    const header = new fabric.IText('PITCH: [Project Name]  |  Appetite: 6 weeks', {
        left: x + 20, top: y + 15,
        fontSize: 18, fontWeight: 'bold',
        fill: color
    });

    const sections = [
        { x: x + 10, y: y + 55, w: 335, h: 150, title: '1. PROBLEM', content: '[Problem description]' },
        { x: x + 355, y: y + 55, w: 335, h: 150, title: '2. SOLUTION', content: '[Solution overview]' },
        { x: x + 10, y: y + 215, w: 335, h: 120, title: '3. RABBIT HOLES', content: '• [Risk 1]\n• [Risk 2]' },
        { x: x + 355, y: y + 215, w: 335, h: 120, title: '4. NO-GOS', content: '✗ [Excluded 1]\n✗ [Excluded 2]' },
        { x: x + 10, y: y + 345, w: 680, h: 130, title: '5. FLOW', content: '[Start] → [Middle] → [End]' }
    ];

    const objects = [frame, header];

    sections.forEach(s => {
        const box = new fabric.Rect({
            left: s.x, top: s.y,
            width: s.w, height: s.h,
            fill: '#f8f9fa',
            stroke: '#dee2e6',
            strokeWidth: 1,
            rx: 6, ry: 6
        });
        const titleText = new fabric.IText(s.title, {
            left: s.x + 10, top: s.y + 10,
            fontSize: 13, fontWeight: 'bold',
            fill: color
        });
        const content = new fabric.IText(s.content, {
            left: s.x + 10, top: s.y + 35,
            fontSize: 11,
            fill: '#495057'
        });
        objects.push(box, titleText, content);
    });

    const group = new fabric.Group(objects, { selectable: true });
    canvas.add(group);
    canvas.renderAll();
}

// ============================================================================
// AI Generation
// ============================================================================
const PROMPT_MAX_LENGTH = 2000;

// Summarize long prompt for wireframe generation
async function summarizePromptIfNeeded(prompt) {
    if (prompt.length <= PROMPT_MAX_LENGTH) {
        return { prompt, wasSummarized: false };
    }

    try {
        showProgress('프롬프트 요약 중...');
        const response = await fetch('/api/shapeup/summarize', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ prompt })
        });

        if (!response.ok) {
            console.warn('Failed to summarize prompt, using original');
            return { prompt, wasSummarized: false };
        }

        const result = await response.json();
        const summarizedPrompt = result.summary || prompt;

        return {
            prompt: summarizedPrompt,
            wasSummarized: true,
            originalLength: prompt.length,
            summarizedLength: summarizedPrompt.length
        };
    } catch (error) {
        console.error('Summarization error:', error);
        return { prompt, wasSummarized: false };
    }
}

async function generateWithAI() {
    let prompt = document.getElementById('ai-prompt').value.trim();
    if (!prompt) {
        alert('Please enter a prompt to generate a board.');
        return;
    }

    const boardType = document.getElementById('board-type').value;
    const btn = document.getElementById('btn-generate');

    if (boardType === 'freeboard') {
        await generateFreeBoard(prompt, btn);
    } else {
        await generateSingleBoard(prompt, boardType, btn);
    }
}

// Free Board generation - creates custom visualization boards with optional memory search
async function generateFreeBoard(prompt, btn) {
    btn.disabled = true;
    btn.innerHTML = '<span class="loading-spinner"></span> Generating...';

    try {
        // Check if prompt needs summarization (> 2000 chars)
        const summarizeResult = await summarizePromptIfNeeded(prompt);
        let finalPrompt = summarizeResult.prompt;

        if (summarizeResult.wasSummarized) {
            showToast('info', '프롬프트 요약됨',
                `프롬프트가 ${summarizeResult.originalLength}자에서 ${summarizeResult.summarizedLength}자로 요약되었습니다.`);
        }

        // Get memory search option
        const useMemorySearch = document.getElementById('use-memory-search')?.checked || false;

        if (useMemorySearch) {
            showProgress('메모리 검색 준비 중...');
        } else {
            showProgress('보드 생성 중...');
        }

        const response = await fetch('/api/shapeup/generate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({
                prompt: finalPrompt,
                boardType: 'freeboard',
                useMemorySearch: useMemorySearch
            })
        });

        if (!response.ok) throw new Error('Generation failed');

        let fullContent = '';
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            buffer += decoder.decode(value, { stream: true });
            const lines = buffer.split('\n');
            buffer = lines.pop() || '';

            for (const line of lines) {
                if (line.startsWith('data:')) {
                    try {
                        const data = JSON.parse(line.substring(5).trim());

                        // Handle phase events (for memory search progress)
                        if (data.phase) {
                            showProgress(data.message || '처리 중...');
                        }
                        // Handle memory found notification
                        else if (data.searchedCount !== undefined || data.adoptedCount !== undefined) {
                            showMemoryToast(data);
                        }
                        // Handle content chunks
                        else if (data.content) {
                            fullContent += data.content;
                        }
                    } catch (e) {}
                }
            }
        }

        hideProgress();

        // Clear canvas and render the free board
        canvas.clear();
        canvas.backgroundColor = '#f8f9fa';
        renderFreeBoard(fullContent);

    } catch (error) {
        console.error('Generation error:', error);
        hideProgress();
        alert('Failed to generate board. Please try again.');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="fas fa-magic me-1"></i> Generate';
    }
}

// Remove comments from JSON string (LLM sometimes adds comments)
function removeJsonComments(str) {
    // Remove single-line comments (// ...)
    // Be careful not to remove // inside strings
    let result = '';
    let inString = false;
    let escape = false;
    let i = 0;

    while (i < str.length) {
        const char = str[i];
        const nextChar = str[i + 1];

        if (escape) {
            result += char;
            escape = false;
            i++;
            continue;
        }

        if (char === '\\' && inString) {
            result += char;
            escape = true;
            i++;
            continue;
        }

        if (char === '"') {
            inString = !inString;
            result += char;
            i++;
            continue;
        }

        if (!inString) {
            // Check for single-line comment
            if (char === '/' && nextChar === '/') {
                // Skip until end of line
                while (i < str.length && str[i] !== '\n') {
                    i++;
                }
                continue;
            }
            // Check for multi-line comment
            if (char === '/' && nextChar === '*') {
                i += 2; // Skip /*
                // Skip until */
                while (i < str.length - 1) {
                    if (str[i] === '*' && str[i + 1] === '/') {
                        i += 2; // Skip */
                        break;
                    }
                    i++;
                }
                continue;
            }
        }

        result += char;
        i++;
    }

    return result;
}

// Extract JSON from LLM response with multiple strategies
function extractJsonFromContent(content) {
    if (!content || typeof content !== 'string') {
        return null;
    }

    // First, remove any comments from the content
    const cleanContent = removeJsonComments(content);

    // Strategy 1: Extract from markdown code block (```json ... ``` or ``` ... ```)
    const codeBlockMatch = cleanContent.match(/```(?:json)?\s*([\s\S]*?)```/);
    if (codeBlockMatch) {
        const extracted = codeBlockMatch[1].trim();
        if (extracted.startsWith('{')) {
            try {
                JSON.parse(extracted);
                return extracted;
            } catch (e) {
                console.log('Code block JSON parse failed, trying other strategies');
            }
        }
    }

    // Strategy 2: Find JSON object starting with {"boardType" or {"elements"
    const specificMatch = cleanContent.match(/\{[\s\S]*?"(?:boardType|elements)"[\s\S]*\}/);
    if (specificMatch) {
        try {
            // Find balanced braces
            const jsonStr = extractBalancedJson(specificMatch[0]);
            if (jsonStr) {
                JSON.parse(jsonStr);
                return jsonStr;
            }
        } catch (e) {
            console.log('Specific pattern JSON parse failed');
        }
    }

    // Strategy 3: Find first { and try to find matching }
    const firstBrace = cleanContent.indexOf('{');
    if (firstBrace !== -1) {
        const jsonStr = extractBalancedJson(cleanContent.substring(firstBrace));
        if (jsonStr) {
            try {
                JSON.parse(jsonStr);
                return jsonStr;
            } catch (e) {
                console.log('Balanced brace extraction failed');
            }
        }
    }

    // Strategy 4: Original greedy match (fallback)
    const greedyMatch = cleanContent.match(/\{[\s\S]*\}/);
    if (greedyMatch) {
        try {
            JSON.parse(greedyMatch[0]);
            return greedyMatch[0];
        } catch (e) {
            console.log('Greedy match JSON parse failed');
        }
    }

    return null;
}

// Extract balanced JSON by counting braces
function extractBalancedJson(str) {
    let depth = 0;
    let start = -1;
    let inString = false;
    let escape = false;

    for (let i = 0; i < str.length; i++) {
        const char = str[i];

        if (escape) {
            escape = false;
            continue;
        }

        if (char === '\\' && inString) {
            escape = true;
            continue;
        }

        if (char === '"' && !escape) {
            inString = !inString;
            continue;
        }

        if (inString) continue;

        if (char === '{') {
            if (depth === 0) start = i;
            depth++;
        } else if (char === '}') {
            depth--;
            if (depth === 0 && start !== -1) {
                return str.substring(start, i + 1);
            }
        }
    }

    return null;
}

// Render Free Board elements from JSON
function renderFreeBoard(content) {
    try {
        // Try to extract JSON from the content with multiple strategies
        let jsonStr = extractJsonFromContent(content);
        if (!jsonStr) {
            throw new Error('No valid JSON found in response');
        }

        const boardData = JSON.parse(jsonStr);
        const elements = boardData.elements || [];

        // Element map for ID-based arrow connections
        const elementMap = {};
        const deferredArrows = [];

        // Helper function to calculate anchor point
        function getAnchorPoint(elementInfo, anchor) {
            const { x, y, width, height, type, radius } = elementInfo;

            // For circles
            if (type === 'circle') {
                const r = radius || 30;
                const cx = x + r;
                const cy = y + r;
                switch (anchor) {
                    case 'left': return { x: x, y: cy };
                    case 'right': return { x: x + r * 2, y: cy };
                    case 'top': return { x: cx, y: y };
                    case 'bottom': return { x: cx, y: y + r * 2 };
                    default: return { x: cx, y: cy };
                }
            }

            // For rectangles and other elements
            const w = width || 100;
            const h = height || 60;
            switch (anchor) {
                case 'left': return { x: x, y: y + h / 2 };
                case 'right': return { x: x + w, y: y + h / 2 };
                case 'top': return { x: x + w / 2, y: y };
                case 'bottom': return { x: x + w / 2, y: y + h };
                case 'center': return { x: x + w / 2, y: y + h / 2 };
                default: return { x: x + w, y: y + h / 2 }; // default to right
            }
        }

        // Helper function to render arrow
        function renderArrow(el, fromX, fromY, toX, toY) {
            const line = new fabric.Line([fromX, fromY, toX, toY], {
                stroke: el.color || el.stroke || '#333333',
                strokeWidth: el.strokeWidth || 2
            });
            canvas.add(line);

            // Add arrowhead
            if (el.type === 'arrow' || el.type === 'connection') {
                const angle = Math.atan2(toY - fromY, toX - fromX) * 180 / Math.PI;
                const arrowHead = new fabric.Triangle({
                    left: toX,
                    top: toY,
                    width: 12,
                    height: 12,
                    fill: el.color || el.stroke || '#333333',
                    angle: angle + 90,
                    originX: 'center',
                    originY: 'center'
                });
                canvas.add(arrowHead);
            }

            if (el.label) {
                const midX = (fromX + toX) / 2;
                const midY = (fromY + toY) / 2;
                const lineLabel = new fabric.IText(el.label, {
                    left: midX,
                    top: midY - 15,
                    fontSize: 11,
                    fill: '#666666',
                    backgroundColor: 'rgba(255,255,255,0.8)'
                });
                canvas.add(lineLabel);
            }
        }

        // Helper function to register element in map
        function registerElement(el, extraHeight) {
            if (el.id) {
                elementMap[el.id] = {
                    x: el.x || 0,
                    y: el.y || 0,
                    width: el.width || 100,
                    height: extraHeight || el.height || 60,
                    type: el.type,
                    radius: el.radius
                };
            }
        }

        elements.forEach(el => {
            switch (el.type) {
                case 'rect':
                case 'box':
                    registerElement(el);
                    const rect = new fabric.Rect({
                        left: el.x || 0,
                        top: el.y || 0,
                        width: el.width || 100,
                        height: el.height || 60,
                        fill: el.fill || '#ffffff',
                        stroke: el.stroke || '#333333',
                        strokeWidth: el.strokeWidth || 2,
                        rx: el.rx || 8,
                        ry: el.ry || 8
                    });
                    canvas.add(rect);

                    if (el.label || el.text) {
                        const label = new fabric.IText(el.label || el.text, {
                            left: (el.x || 0) + 10,
                            top: (el.y || 0) + 10,
                            fontSize: el.fontSize || 14,
                            fill: el.textColor || '#333333'
                        });
                        canvas.add(label);
                    }
                    break;

                case 'text':
                case 'title':
                case 'label':
                    registerElement(el, 30);
                    const text = new fabric.IText(el.text || el.content || '', {
                        left: el.x || 0,
                        top: el.y || 0,
                        fontSize: el.fontSize || (el.type === 'title' ? 24 : 14),
                        fontWeight: el.fontWeight || (el.type === 'title' ? 'bold' : 'normal'),
                        fill: el.color || el.fill || '#333333'
                    });
                    canvas.add(text);
                    break;

                case 'circle':
                    registerElement(el);
                    const circle = new fabric.Circle({
                        left: el.x || 0,
                        top: el.y || 0,
                        radius: el.radius || 30,
                        fill: el.fill || '#ffffff',
                        stroke: el.stroke || '#333333',
                        strokeWidth: el.strokeWidth || 2
                    });
                    canvas.add(circle);

                    if (el.label || el.text) {
                        const circleLabel = new fabric.IText(el.label || el.text, {
                            left: (el.x || 0) + (el.radius || 30) - 20,
                            top: (el.y || 0) + (el.radius || 30) - 8,
                            fontSize: 12,
                            fill: '#333333'
                        });
                        canvas.add(circleLabel);
                    }
                    break;

                case 'arrow':
                case 'connection':
                case 'connector':
                case 'line':
                    // Check if ID-based connection
                    if (el.fromId && el.toId) {
                        deferredArrows.push(el);
                    } else {
                        // Fallback to coordinate-based
                        const fromX = el.fromX || el.x1 || 0;
                        const fromY = el.fromY || el.y1 || 0;
                        const toX = el.toX || el.x2 || 100;
                        const toY = el.toY || el.y2 || 0;
                        renderArrow(el, fromX, fromY, toX, toY);
                    }
                    break;

                case 'group':
                case 'frame':
                    registerElement(el);
                    const frameRect = new fabric.Rect({
                        left: el.x || 0,
                        top: el.y || 0,
                        width: el.width || 300,
                        height: el.height || 200,
                        fill: el.fill || '#ffffff',
                        stroke: el.color || el.stroke || '#667eea',
                        strokeWidth: 3,
                        rx: 12,
                        ry: 12
                    });
                    canvas.add(frameRect);

                    if (el.title) {
                        const frameTitle = new fabric.IText(el.title, {
                            left: (el.x || 0) + 15,
                            top: (el.y || 0) + 12,
                            fontSize: 16,
                            fontWeight: 'bold',
                            fill: el.color || '#667eea'
                        });
                        canvas.add(frameTitle);
                    }
                    break;

                case 'section':
                    registerElement(el);
                    const sectionRect = new fabric.Rect({
                        left: el.x || 0,
                        top: el.y || 0,
                        width: el.width || 200,
                        height: el.height || 150,
                        fill: el.fill || '#f8f9fa',
                        stroke: el.stroke || '#dee2e6',
                        strokeWidth: 2,
                        rx: 8,
                        ry: 8
                    });
                    canvas.add(sectionRect);

                    if (el.title) {
                        const sectionTitle = new fabric.IText(el.title, {
                            left: (el.x || 0) + 10,
                            top: (el.y || 0) + 8,
                            fontSize: 14,
                            fontWeight: 'bold',
                            fill: '#495057'
                        });
                        canvas.add(sectionTitle);
                    }

                    if (el.content) {
                        const sectionContent = new fabric.IText(el.content, {
                            left: (el.x || 0) + 10,
                            top: (el.y || 0) + 30,
                            fontSize: 12,
                            fill: '#6c757d',
                            width: (el.width || 200) - 20
                        });
                        canvas.add(sectionContent);
                    }
                    break;

                case 'note':
                case 'sticky':
                    registerElement(el);
                    const noteColor = el.color || el.fill || '#fff9c4';
                    const noteRect = new fabric.Rect({
                        left: el.x || 0,
                        top: el.y || 0,
                        width: el.width || 150,
                        height: el.height || 100,
                        fill: noteColor,
                        stroke: noteColor === '#fff9c4' ? '#fbc02d' : noteColor,
                        strokeWidth: 1,
                        rx: 4,
                        ry: 4
                    });
                    canvas.add(noteRect);

                    if (el.text || el.content) {
                        const noteText = new fabric.IText(el.text || el.content, {
                            left: (el.x || 0) + 10,
                            top: (el.y || 0) + 10,
                            fontSize: 12,
                            fill: '#333333',
                            width: (el.width || 150) - 20
                        });
                        canvas.add(noteText);
                    }
                    break;

                case 'entity':
                case 'table':
                    // Calculate height based on fields
                    const entityHeight = el.height || (30 + (el.fields ? el.fields.length * 18 + 10 : 60));
                    registerElement(el, entityHeight);

                    // For ERD-like diagrams
                    const entityRect = new fabric.Rect({
                        left: el.x || 0,
                        top: el.y || 0,
                        width: el.width || 180,
                        height: entityHeight,
                        fill: '#ffffff',
                        stroke: el.stroke || '#2196f3',
                        strokeWidth: 2,
                        rx: 4,
                        ry: 4
                    });
                    canvas.add(entityRect);

                    // Entity title bar
                    const titleBar = new fabric.Rect({
                        left: el.x || 0,
                        top: el.y || 0,
                        width: el.width || 180,
                        height: 30,
                        fill: el.stroke || '#2196f3',
                        rx: 4,
                        ry: 4
                    });
                    canvas.add(titleBar);

                    const entityTitle = new fabric.IText(el.name || el.title || 'Entity', {
                        left: (el.x || 0) + 10,
                        top: (el.y || 0) + 7,
                        fontSize: 14,
                        fontWeight: 'bold',
                        fill: '#ffffff'
                    });
                    canvas.add(entityTitle);

                    // Entity fields
                    if (el.fields && Array.isArray(el.fields)) {
                        el.fields.forEach((field, idx) => {
                            const fieldText = new fabric.IText(field, {
                                left: (el.x || 0) + 10,
                                top: (el.y || 0) + 35 + (idx * 18),
                                fontSize: 12,
                                fill: '#333333'
                            });
                            canvas.add(fieldText);
                        });
                    }
                    break;

                case 'icon':
                    const iconText = new fabric.IText(el.icon || '📌', {
                        left: el.x || 0,
                        top: el.y || 0,
                        fontSize: el.size || 24
                    });
                    canvas.add(iconText);
                    break;

                case 'svgbox':
                    // SVG Box with custom path support
                    registerElement(el);
                    const svgBoxX = el.x || 0;
                    const svgBoxY = el.y || 0;
                    const svgBoxWidth = el.width || 100;
                    const svgBoxHeight = el.height || 80;
                    const svgBoxStroke = el.stroke || el.color || '#333333';
                    const svgBoxStrokeWidth = el.strokeWidth || 2;

                    if (el.path) {
                        // Render custom SVG path
                        const svgString = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                            <path d="${el.path}" fill="none" stroke="${svgBoxStroke}" stroke-width="${svgBoxStrokeWidth}" stroke-linecap="round" stroke-linejoin="round"/>
                        </svg>`;

                        fabric.loadSVGFromString(svgString, function(objects, options) {
                            if (objects && objects.length > 0) {
                                const svgGroup = fabric.util.groupSVGElements(objects, options);

                                // Scale to fit the box
                                const scaleX = svgBoxWidth / 24;
                                const scaleY = svgBoxHeight / 24;
                                const scale = Math.min(scaleX, scaleY) * 0.8;

                                svgGroup.set({
                                    left: svgBoxX + svgBoxWidth / 2,
                                    top: svgBoxY + svgBoxHeight / 2,
                                    scaleX: scale,
                                    scaleY: scale,
                                    originX: 'center',
                                    originY: 'center',
                                    objectType: 'svgbox',
                                    svgPath: el.path
                                });

                                // Add dashed border frame
                                const frame = new fabric.Rect({
                                    left: svgBoxX,
                                    top: svgBoxY,
                                    width: svgBoxWidth,
                                    height: svgBoxHeight,
                                    fill: 'transparent',
                                    stroke: svgBoxStroke,
                                    strokeWidth: 1,
                                    strokeDashArray: [4, 4],
                                    selectable: false,
                                    evented: false
                                });

                                const group = new fabric.Group([frame, svgGroup], {
                                    left: svgBoxX,
                                    top: svgBoxY,
                                    objectType: 'svgbox',
                                    svgPath: el.path
                                });

                                canvas.add(group);
                                canvas.renderAll();
                            }
                        });
                    } else {
                        // Fallback: simple rectangle with border
                        const svgBox = new fabric.Rect({
                            left: svgBoxX,
                            top: svgBoxY,
                            width: svgBoxWidth,
                            height: svgBoxHeight,
                            fill: 'transparent',
                            stroke: svgBoxStroke,
                            strokeWidth: svgBoxStrokeWidth,
                            rx: el.rx || 0,
                            ry: el.ry || 0,
                            objectType: 'svgbox'
                        });
                        canvas.add(svgBox);

                        if (el.label || el.text) {
                            const svgBoxLabel = new fabric.IText(el.label || el.text, {
                                left: svgBoxX + 10,
                                top: svgBoxY + 10,
                                fontSize: el.fontSize || 12,
                                fill: el.textColor || '#333333'
                            });
                            canvas.add(svgBoxLabel);
                        }
                    }
                    break;

                case 'svgicon':
                    // SVG Icon from library
                    if (el.iconId && typeof addSvgIcon === 'function') {
                        addSvgIcon(el.iconId, el.x || 0, el.y || 0, el.size || 48);
                    } else if (el.path) {
                        // Custom SVG path
                        const customPath = new fabric.Path(el.path, {
                            left: el.x || 0,
                            top: el.y || 0,
                            fill: el.fill || 'transparent',
                            stroke: el.stroke || el.color || '#333333',
                            strokeWidth: el.strokeWidth || 2,
                            scaleX: el.scale || 1,
                            scaleY: el.scale || 1,
                            objectType: 'svgicon'
                        });
                        canvas.add(customPath);
                    }
                    break;

                default:
                    // Default: render as a simple box with text
                    if (el.x !== undefined && el.y !== undefined) {
                        registerElement(el);
                        const defaultRect = new fabric.Rect({
                            left: el.x,
                            top: el.y,
                            width: el.width || 100,
                            height: el.height || 50,
                            fill: el.fill || '#f0f0f0',
                            stroke: el.stroke || '#999999',
                            strokeWidth: 1,
                            rx: 4,
                            ry: 4
                        });
                        canvas.add(defaultRect);

                        if (el.text || el.label || el.content) {
                            const defaultText = new fabric.IText(el.text || el.label || el.content, {
                                left: el.x + 5,
                                top: el.y + 5,
                                fontSize: 12,
                                fill: '#333333'
                            });
                            canvas.add(defaultText);
                        }
                    }
            }
        });

        // Process deferred arrows with ID-based connections
        deferredArrows.forEach(el => {
            const fromElement = elementMap[el.fromId];
            const toElement = elementMap[el.toId];

            if (fromElement && toElement) {
                const fromPoint = getAnchorPoint(fromElement, el.fromAnchor || 'right');
                const toPoint = getAnchorPoint(toElement, el.toAnchor || 'left');
                renderArrow(el, fromPoint.x, fromPoint.y, toPoint.x, toPoint.y);
            } else {
                console.warn(`Arrow connection failed: fromId="${el.fromId}" (${fromElement ? 'found' : 'not found'}), toId="${el.toId}" (${toElement ? 'found' : 'not found'})`);
                // Fallback to coordinate-based if available
                if (el.fromX !== undefined && el.toX !== undefined) {
                    renderArrow(el, el.fromX, el.fromY, el.toX, el.toY);
                }
            }
        });

        canvas.renderAll();
    } catch (error) {
        console.error('Failed to parse Free Board JSON:', error);
        // Fallback: display the raw content as text
        const fallbackText = new fabric.IText('Generated content:\n\n' + content.substring(0, 500), {
            left: 50,
            top: 50,
            fontSize: 14,
            fill: '#333333'
        });
        canvas.add(fallbackText);
        canvas.renderAll();
    }
}

// Single board generation
async function generateSingleBoard(prompt, boardType, btn) {
    btn.disabled = true;
    btn.innerHTML = '<span class="loading-spinner"></span> Generating...';

    try {
        // Check if prompt needs summarization (> 2000 chars)
        const summarizeResult = await summarizePromptIfNeeded(prompt);
        let finalPrompt = summarizeResult.prompt;

        if (summarizeResult.wasSummarized) {
            showToast('info', '프롬프트 요약됨',
                `프롬프트가 ${summarizeResult.originalLength}자에서 ${summarizeResult.summarizedLength}자로 요약되었습니다.`);
        }

        showProgress('보드 생성 중...');

        const response = await fetch('/api/shapeup/generate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ prompt: finalPrompt, boardType })
        });

        if (!response.ok) throw new Error('Generation failed');

        let fullContent = '';
        const reader = response.body.getReader();
        const decoder = new TextDecoder();

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            const chunk = decoder.decode(value, { stream: true });
            const lines = chunk.split('\n');

            for (const line of lines) {
                if (line.startsWith('data:')) {
                    try {
                        const data = JSON.parse(line.substring(5).trim());
                        if (data.content) {
                            fullContent += data.content;
                        }
                    } catch (e) {}
                }
            }
        }

        hideProgress();

        // Try to parse and render the generated board
        renderGeneratedBoard(fullContent, boardType);

    } catch (error) {
        console.error('Generation error:', error);
        hideProgress();
        alert('Failed to generate board. Please try again.');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="fas fa-magic me-1"></i> Generate';
    }
}

// Integrated board generation (5 boards step by step)
async function generateIntegratedBoards(prompt, btn) {
    const steps = [
        { type: 'problem', label: 'Problem', offset: { x: 50, y: 50 } },
        { type: 'breadboard', label: 'Breadboard', offset: { x: 700, y: 50 } },
        { type: 'fat-marker', label: 'Fat Marker', offset: { x: 50, y: 480 } },
        { type: 'risk', label: 'Risk', offset: { x: 500, y: 480 } },
        { type: 'pitch', label: 'Pitch', offset: { x: 1250, y: 50 } }
    ];

    const stepResults = {};
    btn.disabled = true;

    // Clear canvas for integrated view
    canvas.clear();
    canvas.backgroundColor = '#f8f9fa';
    canvas.renderAll();

    for (let i = 0; i < steps.length; i++) {
        const step = steps[i];
        const progress = `${i + 1}/${steps.length}`;
        btn.innerHTML = `<span class="loading-spinner"></span> ${step.label} (${progress})`;

        try {
            // Build context from previous steps
            let contextPrompt = prompt;
            if (i > 0) {
                contextPrompt = buildIntegratedPrompt(prompt, step.type, stepResults);
            }

            const response = await fetch('/api/shapeup/generate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({
                    prompt: contextPrompt,
                    boardType: step.type,
                    isIntegrated: true,
                    previousResults: i > 0 ? stepResults : null
                })
            });

            if (!response.ok) throw new Error(`${step.label} generation failed`);

            let fullContent = '';
            const reader = response.body.getReader();
            const decoder = new TextDecoder();

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const chunk = decoder.decode(value, { stream: true });
                const lines = chunk.split('\n');

                for (const line of lines) {
                    if (line.startsWith('data:')) {
                        try {
                            const data = JSON.parse(line.substring(5).trim());
                            if (data.content) {
                                fullContent += data.content;
                            }
                        } catch (e) {}
                    }
                }
            }

            // Store result for next step
            stepResults[step.type] = fullContent;

            // Render board at specific offset
            renderIntegratedBoard(fullContent, step.type, step.offset);

        } catch (error) {
            console.error(`${step.label} generation error:`, error);
            // Continue with next step even if one fails
        }
    }

    btn.disabled = false;
    btn.innerHTML = '<i class="fas fa-magic me-1"></i> Generate';

    // Fit all boards in view
    fitAllBoardsInView();
}

// Build context prompt with previous step results
function buildIntegratedPrompt(originalPrompt, currentType, previousResults) {
    let context = `## 원래 요구사항\n${originalPrompt}\n\n`;

    if (previousResults.problem) {
        context += `## 이전 단계: Problem Definition 결과\n`;
        context += extractSummaryFromResult(previousResults.problem, 'problem') + '\n\n';
    }

    if (previousResults.breadboard && (currentType === 'fat-marker' || currentType === 'risk' || currentType === 'pitch')) {
        context += `## 이전 단계: Breadboard 결과\n`;
        context += extractSummaryFromResult(previousResults.breadboard, 'breadboard') + '\n\n';
    }

    if (previousResults['fat-marker'] && (currentType === 'risk' || currentType === 'pitch')) {
        context += `## 이전 단계: Fat Marker Sketch 결과\n`;
        context += extractSummaryFromResult(previousResults['fat-marker'], 'fat-marker') + '\n\n';
    }

    if (previousResults.risk && currentType === 'pitch') {
        context += `## 이전 단계: Risk Assessment 결과\n`;
        context += extractSummaryFromResult(previousResults.risk, 'risk') + '\n\n';
    }

    context += `\n위의 이전 단계 결과들을 참고하여 일관성 있게 ${currentType} 보드를 생성해주세요.`;
    return context;
}

// Extract summary from JSON result
function extractSummaryFromResult(content, type) {
    try {
        const jsonMatch = content.match(/\{[\s\S]*\}/);
        if (!jsonMatch) return content.substring(0, 500);

        const data = JSON.parse(jsonMatch[0]);
        const elements = data.elements || [];

        let summary = '';
        elements.forEach(el => {
            if (el.title) summary += `- ${el.title}: `;
            if (el.content) summary += el.content + '\n';
            if (el.name) summary += el.name + '\n';
            if (el.items && Array.isArray(el.items)) {
                el.items.forEach(item => {
                    if (typeof item === 'string') summary += `  • ${item}\n`;
                    else if (item.description) summary += `  • ${item.description}\n`;
                    else if (item.text) summary += `  • ${item.text}\n`;
                });
            }
        });

        return summary || content.substring(0, 500);
    } catch (e) {
        return content.substring(0, 500);
    }
}

// Render board at specific offset for integrated view
function renderIntegratedBoard(content, boardType, offset) {
    if (!canvas) return;

    try {
        let jsonMatch = content.match(/\{[\s\S]*\}/);
        if (!jsonMatch) {
            addBoardTemplateAtOffset(boardType, offset);
            return;
        }

        const boardData = JSON.parse(jsonMatch[0]);

        if (boardData.elements && Array.isArray(boardData.elements)) {
            // Adjust element positions with offset
            const adjustedElements = boardData.elements.map(el => ({
                ...el,
                x: (el.x || 0) + offset.x,
                y: (el.y || 0) + offset.y
            }));
            renderBoardElements(adjustedElements, boardData.title || boardType);
        } else {
            addBoardTemplateAtOffset(boardType, offset);
        }
    } catch (e) {
        console.error('Parse error for', boardType, e);
        addBoardTemplateAtOffset(boardType, offset);
    }
}

// Add template at specific offset
function addBoardTemplateAtOffset(type, offset) {
    const colors = {
        'problem': '#667eea',
        'breadboard': '#00c6ff',
        'fat-marker': '#11998e',
        'risk': '#f093fb',
        'pitch': '#ff6b6b'
    };

    const color = colors[type] || '#667eea';

    switch (type) {
        case 'problem':
            addProblemBoard(offset.x, offset.y, color);
            break;
        case 'breadboard':
            addBreadboard(offset.x, offset.y, color);
            break;
        case 'fat-marker':
            addFatMarkerSketch(offset.x, offset.y, color);
            break;
        case 'risk':
            addRiskBoard(offset.x, offset.y, color);
            break;
        case 'pitch':
            addPitchBoard(offset.x, offset.y, color);
            break;
    }
}

// Fit all boards in view after integrated generation
function fitAllBoardsInView() {
    if (!canvas) return;

    // Get bounding box of all objects
    const objects = canvas.getObjects();
    if (objects.length === 0) return;

    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;

    objects.forEach(obj => {
        const bound = obj.getBoundingRect();
        minX = Math.min(minX, bound.left);
        minY = Math.min(minY, bound.top);
        maxX = Math.max(maxX, bound.left + bound.width);
        maxY = Math.max(maxY, bound.top + bound.height);
    });

    const contentWidth = maxX - minX;
    const contentHeight = maxY - minY;
    const canvasWidth = canvas.width;
    const canvasHeight = canvas.height;

    // Calculate zoom to fit
    const scaleX = (canvasWidth - 100) / contentWidth;
    const scaleY = (canvasHeight - 100) / contentHeight;
    const scale = Math.min(scaleX, scaleY, 1); // Don't zoom in beyond 100%

    // Set zoom and center
    zoomLevel = scale;
    canvas.setZoom(scale);

    // Center the content
    const centerX = minX + contentWidth / 2;
    const centerY = minY + contentHeight / 2;
    const vpCenterX = canvasWidth / 2 / scale;
    const vpCenterY = canvasHeight / 2 / scale;

    canvas.viewportTransform[4] = (vpCenterX - centerX) * scale;
    canvas.viewportTransform[5] = (vpCenterY - centerY) * scale;

    canvas.renderAll();
    updateZoomDisplay();
}

function renderGeneratedBoard(content, boardType) {
    // Ensure canvas is initialized
    if (!canvas) {
        console.error('Canvas not initialized');
        alert('Canvas not ready. Please refresh and try again.');
        return;
    }

    try {
        // Try to extract JSON from the content with multiple strategies
        let jsonStr = extractJsonFromContent(content);
        if (!jsonStr) {
            console.error('No JSON found in response');
            addBoardTemplate(boardType);
            alert('Template added. AI response could not be parsed.');
            return;
        }

        const boardData = JSON.parse(jsonStr);
        console.log('Parsed board data:', boardData);

        if (boardData.elements && Array.isArray(boardData.elements)) {
            renderBoardElements(boardData.elements, boardData.title || 'Shape Up Board');
        } else {
            addBoardTemplate(boardType);
            alert('Template added. AI response format not recognized.');
        }
    } catch (e) {
        console.error('Parse error:', e);
        addBoardTemplate(boardType);
        alert('Template added. Edit it to match your requirements.');
    }
}

// Render AI-generated board elements
function renderBoardElements(elements, title) {
    const objects = [];
    const placeMap = {}; // For breadboard connections

    elements.forEach(el => {
        switch (el.type) {
            case 'frame':
                objects.push(createFrame(el));
                break;
            case 'section':
            case 'pitchSection':
                objects.push(...createSection(el));
                break;
            case 'place':
                const placeObjs = createPlace(el);
                objects.push(...placeObjs);
                if (el.id) placeMap[el.id] = el;
                break;
            case 'box':
                objects.push(...createBox(el));
                break;
            case 'squiggle':
                objects.push(createSquiggle(el));
                break;
            case 'annotation':
                objects.push(createAnnotation(el));
                break;
            case 'appetite':
                objects.push(...createAppetite(el));
                break;
            case 'rabbitHoleList':
                objects.push(...createRabbitHoleList(el));
                break;
            case 'noGoList':
                objects.push(...createNoGoList(el));
                break;
            case 'pitchHeader':
                objects.push(...createPitchHeader(el));
                break;
            case 'breadboardMini':
                objects.push(...createBreadboardMini(el));
                break;
            case 'riskCurve':
                objects.push(...createRiskCurve(el));
                break;
        }
    });

    // Add connection lines for breadboard
    elements.filter(el => el.type === 'connection').forEach(conn => {
        const fromPlace = placeMap[conn.from];
        const toPlace = placeMap[conn.to];
        if (fromPlace && toPlace) {
            objects.push(...createConnection(fromPlace, toPlace, conn.label));
        }
    });

    // Add all objects to canvas
    objects.forEach(obj => canvas.add(obj));
    canvas.renderAll();
}

// Helper: Create frame
function createFrame(el) {
    return new fabric.Rect({
        left: el.x || 50,
        top: el.y || 50,
        width: el.width || 600,
        height: el.height || 400,
        fill: 'white',
        stroke: el.color || '#667eea',
        strokeWidth: 3,
        rx: 12,
        ry: 12,
        selectable: true
    });
}

// Helper: Create section with title and content (auto-wrap)
function createSection(el) {
    const objs = [];
    const boxWidth = el.width || 200;
    const boxHeight = el.height || 100;
    const box = new fabric.Rect({
        left: el.x || 0,
        top: el.y || 0,
        width: boxWidth,
        height: boxHeight,
        fill: '#f8f9fa',
        stroke: '#dee2e6',
        strokeWidth: 1,
        rx: 6,
        ry: 6
    });
    objs.push(box);

    if (el.title) {
        objs.push(new fabric.Text(el.title, {
            left: (el.x || 0) + 10,
            top: (el.y || 0) + 10,
            fontSize: 12,
            fontWeight: 'bold',
            fill: el.color || '#667eea'
        }));
    }

    if (el.content) {
        // Use Textbox for auto-wrapping
        objs.push(new fabric.Textbox(el.content, {
            left: (el.x || 0) + 10,
            top: (el.y || 0) + 35,
            fontSize: 11,
            fill: '#495057',
            width: boxWidth - 20,
            splitByGrapheme: true
        }));
    }

    // Handle items array (for rabbit holes, no-gos)
    if (el.items && Array.isArray(el.items)) {
        let yOffset = 35;
        el.items.forEach((item, i) => {
            const itemText = typeof item === 'string' ? item :
                (item.text || item.description || JSON.stringify(item));
            const textbox = new fabric.Textbox('• ' + itemText, {
                left: (el.x || 0) + 10,
                top: (el.y || 0) + yOffset,
                fontSize: 11,
                fill: '#495057',
                width: boxWidth - 20,
                splitByGrapheme: true
            });
            objs.push(textbox);
            yOffset += Math.max(20, textbox.height + 5);
        });
    }

    return objs;
}

// Helper: Create place (breadboard) with auto-wrap
function createPlace(el) {
    const objs = [];
    const boxWidth = el.width || 150;
    objs.push(new fabric.Rect({
        left: el.x || 0,
        top: el.y || 0,
        width: boxWidth,
        height: el.height || 120,
        fill: '#f8f9fa',
        stroke: '#333',
        strokeWidth: 2,
        rx: 6,
        ry: 6
    }));

    objs.push(new fabric.Textbox(el.name || 'Place', {
        left: (el.x || 0) + 10,
        top: (el.y || 0) + 10,
        fontSize: 14,
        fontWeight: 'bold',
        fill: '#333',
        underline: true,
        width: boxWidth - 20,
        splitByGrapheme: true
    }));

    if (el.affordances && Array.isArray(el.affordances)) {
        let yOffset = 40;
        el.affordances.forEach((aff, i) => {
            const textbox = new fabric.Textbox('• ' + aff, {
                left: (el.x || 0) + 15,
                top: (el.y || 0) + yOffset,
                fontSize: 12,
                fill: '#495057',
                width: boxWidth - 25,
                splitByGrapheme: true
            });
            objs.push(textbox);
            yOffset += Math.max(18, textbox.height + 3);
        });
    }

    return objs;
}

// Helper: Create box (fat marker)
function createBox(el) {
    const objs = [];
    objs.push(new fabric.Rect({
        left: el.x || 0,
        top: el.y || 0,
        width: el.width || 100,
        height: el.height || 50,
        fill: 'transparent',
        stroke: '#333',
        strokeWidth: 4,
        rx: 4,
        ry: 4
    }));

    if (el.label) {
        objs.push(new fabric.Text(el.label, {
            left: (el.x || 0) + 10,
            top: (el.y || 0) + 10,
            fontSize: 12,
            fontWeight: 'bold',
            fill: '#6c757d'
        }));
    }

    return objs;
}

// Helper: Create squiggle line
function createSquiggle(el) {
    const y = el.y || 0;
    const width = el.width || 100;
    let path = `M ${el.x || 0} ${y}`;
    for (let i = 0; i < width; i += 10) {
        path += ` Q ${(el.x || 0) + i + 5} ${y + (i % 20 === 0 ? -5 : 5)}, ${(el.x || 0) + i + 10} ${y}`;
    }
    return new fabric.Path(path, {
        stroke: '#333',
        strokeWidth: 3,
        fill: ''
    });
}

// Helper: Create annotation
function createAnnotation(el) {
    return new fabric.Text(el.text || '', {
        left: el.x || 0,
        top: el.y || 0,
        fontSize: 12,
        fill: el.color || '#764ba2',
        fontStyle: 'italic'
    });
}

// Helper: Create appetite selector
function createAppetite(el) {
    const objs = [];
    objs.push(new fabric.Rect({
        left: el.x || 0,
        top: el.y || 0,
        width: el.width || 200,
        height: el.height || 100,
        fill: '#f8f9fa',
        stroke: '#dee2e6',
        strokeWidth: 1,
        rx: 6,
        ry: 6
    }));

    objs.push(new fabric.Text(el.title || 'Appetite', {
        left: (el.x || 0) + 10,
        top: (el.y || 0) + 10,
        fontSize: 12,
        fontWeight: 'bold',
        fill: '#6c757d'
    }));

    const options = el.options || ['Small Batch (1-2주)', 'Big Batch (6주)'];
    options.forEach((opt, i) => {
        const isSelected = el.selected === (i === 0 ? 'small' : 'big');
        objs.push(new fabric.Text((isSelected ? '● ' : '○ ') + opt, {
            left: (el.x || 0) + 15,
            top: (el.y || 0) + 40 + i * 25,
            fontSize: 11,
            fill: isSelected ? '#667eea' : '#495057'
        }));
    });

    return objs;
}

// Helper: Create rabbit hole list with auto-wrap
function createRabbitHoleList(el) {
    const objs = [];
    const boxWidth = el.width || 300;
    objs.push(new fabric.Text(el.title || 'Rabbit Holes', {
        left: el.x || 0,
        top: el.y || 0,
        fontSize: 14,
        fontWeight: 'bold',
        fill: '#333'
    }));

    if (el.items && Array.isArray(el.items)) {
        let yOffset = 30;
        el.items.forEach((item, i) => {
            const status = item.status || 'unknown';
            const statusIcon = status === 'patched' ? '✓' : status === 'out-of-bounds' ? '⊘' : '↓';
            const text = `${statusIcon} ${item.description || ''}${item.solution ? ' → ' + item.solution : ''}`;
            const textbox = new fabric.Textbox(text, {
                left: (el.x || 0) + 10,
                top: (el.y || 0) + yOffset,
                fontSize: 11,
                fill: status === 'patched' ? '#28a745' : '#dc3545',
                width: boxWidth - 20,
                splitByGrapheme: true
            });
            objs.push(textbox);
            yOffset += Math.max(22, textbox.height + 5);
        });
    }

    return objs;
}

// Helper: Create no-go list with auto-wrap
function createNoGoList(el) {
    const objs = [];
    const boxWidth = el.width || 300;
    objs.push(new fabric.Text(el.title || 'No-Gos', {
        left: el.x || 0,
        top: el.y || 0,
        fontSize: 14,
        fontWeight: 'bold',
        fill: '#333'
    }));

    if (el.items && Array.isArray(el.items)) {
        let yOffset = 30;
        el.items.forEach((item, i) => {
            const textbox = new fabric.Textbox('✗ ' + item, {
                left: (el.x || 0) + 10,
                top: (el.y || 0) + yOffset,
                fontSize: 11,
                fill: '#dc3545',
                width: boxWidth - 20,
                splitByGrapheme: true
            });
            objs.push(textbox);
            yOffset += Math.max(22, textbox.height + 5);
        });
    }

    return objs;
}

// Helper: Create pitch header with auto-wrap
function createPitchHeader(el) {
    const headerWidth = el.width || 800;
    return [new fabric.Textbox(`PITCH: ${el.projectName || 'Project'}  |  Appetite: ${el.appetite || '6주'}`, {
        left: el.x || 50,
        top: el.y || 50,
        fontSize: 18,
        fontWeight: 'bold',
        fill: '#ff6b6b',
        width: headerWidth,
        splitByGrapheme: true
    })];
}

// Helper: Create breadboard mini (for pitch flow) with auto-wrap
function createBreadboardMini(el) {
    const objs = [];
    const boxWidth = el.width || 600;
    objs.push(new fabric.Rect({
        left: el.x || 0,
        top: el.y || 0,
        width: boxWidth,
        height: el.height || 100,
        fill: '#f8f9fa',
        stroke: '#dee2e6',
        strokeWidth: 1,
        rx: 6,
        ry: 6
    }));

    objs.push(new fabric.Text(el.title || 'FLOW', {
        left: (el.x || 0) + 10,
        top: (el.y || 0) + 10,
        fontSize: 12,
        fontWeight: 'bold',
        fill: '#667eea'
    }));

    if (el.places && Array.isArray(el.places)) {
        const placeWidth = 100;
        const spacing = (boxWidth - 40) / el.places.length;
        el.places.forEach((place, i) => {
            const px = (el.x || 0) + 20 + spacing * i;
            const py = (el.y || 0) + 35;

            objs.push(new fabric.Rect({
                left: px,
                top: py,
                width: placeWidth,
                height: 55,
                fill: 'white',
                stroke: '#333',
                strokeWidth: 1,
                rx: 4,
                ry: 4
            }));

            objs.push(new fabric.Textbox(place.name || `Step ${i + 1}`, {
                left: px + 5,
                top: py + 5,
                fontSize: 10,
                fontWeight: 'bold',
                fill: '#333',
                width: placeWidth - 10,
                splitByGrapheme: true
            }));

            // Arrow to next
            if (i < el.places.length - 1) {
                objs.push(new fabric.Text('→', {
                    left: px + placeWidth + 5,
                    top: py + 18,
                    fontSize: 16,
                    fill: '#333'
                }));
            }
        });
    }

    return objs;
}

// Helper: Create risk curve
function createRiskCurve(el) {
    const objs = [];
    objs.push(new fabric.Text(el.label || 'Risk Distribution', {
        left: el.x || 0,
        top: el.y || 0,
        fontSize: 12,
        fill: '#6c757d'
    }));

    // Simple curve visualization
    const curveType = el.curveType || 'thin-tail';
    const curveText = curveType === 'thin-tail' ? '📊 Thin-tailed (Predictable)' : '📊 Fat-tailed (Uncertain)';
    objs.push(new fabric.Text(curveText, {
        left: (el.x || 0) + 10,
        top: (el.y || 0) + 25,
        fontSize: 11,
        fill: curveType === 'thin-tail' ? '#28a745' : '#dc3545'
    }));

    return objs;
}

// Helper: Create connection line
function createConnection(from, to, label) {
    const objs = [];
    const fromX = (from.x || 0) + (from.width || 150);
    const fromY = (from.y || 0) + (from.height || 120) / 2;
    const toX = to.x || 0;
    const toY = (to.y || 0) + (to.height || 120) / 2;

    objs.push(new fabric.Line([fromX, fromY, toX, toY], {
        stroke: '#333',
        strokeWidth: 2
    }));

    // Arrow head
    objs.push(new fabric.Triangle({
        left: toX,
        top: toY,
        width: 10,
        height: 10,
        fill: '#333',
        angle: 90,
        originX: 'center',
        originY: 'center'
    }));

    if (label) {
        objs.push(new fabric.Text(label, {
            left: (fromX + toX) / 2,
            top: (fromY + toY) / 2 - 15,
            fontSize: 10,
            fill: '#6c757d'
        }));
    }

    return objs;
}

// ============================================================================
// Share Functionality
// ============================================================================
async function shareBoard() {
    const boardData = JSON.stringify(canvas.toJSON());
    const prompt = document.getElementById('ai-prompt').value.trim();

    try {
        // Generate title
        let title = 'Shape Up Board';
        if (prompt) {
            try {
                const titleResponse = await fetch('/api/shapeup/generate-title', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ prompt })
                });
                if (titleResponse.ok) {
                    const titleData = await titleResponse.json();
                    title = titleData.title || 'Shape Up Board';
                }
            } catch (e) {}
        }

        document.getElementById('share-title').value = title;

        // Create share link
        const response = await fetch('/api/shapeup/share', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                title,
                boardData,
                boardType: document.getElementById('board-type').value,
                originalPrompt: prompt
            })
        });

        if (!response.ok) throw new Error('Share failed');

        const data = await response.json();
        currentShareUrl = `${window.location.origin}/ui/shapeup/share/${data.shortCode}`;
        document.getElementById('share-url').value = currentShareUrl;

        new bootstrap.Modal(document.getElementById('shareModal')).show();
    } catch (error) {
        console.error('Share error:', error);
        alert('Failed to create share link.');
    }
}

function copyShareUrl() {
    const input = document.getElementById('share-url');
    navigator.clipboard.writeText(input.value).then(() => {
        document.getElementById('copy-success').classList.remove('d-none');
        setTimeout(() => {
            document.getElementById('copy-success').classList.add('d-none');
        }, 3000);
    });
}

function openSharePage() {
    if (currentShareUrl) {
        window.open(currentShareUrl, '_blank');
    }
}
