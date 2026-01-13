/**
 * ShapeUp Whiteboard JavaScript
 * Fabric.js based canvas drawing application
 */

// Suppress textBaseline 'alphabetical' warnings/errors (Fabric.js internal issue)
(function() {
    const originalWarn = console.warn;
    const originalError = console.error;
    console.warn = function(...args) {
        if (args[0] && typeof args[0] === 'string' && args[0].includes('alphabetical')) return;
        originalWarn.apply(console, args);
    };
    console.error = function(...args) {
        if (args[0] && typeof args[0] === 'string' && args[0].includes('alphabetical')) return;
        originalError.apply(console, args);
    };
})();

// ============================================================================
// Custom Property Serialization for Fabric.js
// ============================================================================
// Add custom properties to Fabric.js serialization for proper share/load
(function() {
    // Custom properties to include in JSON serialization
    const customProperties = ['objectType', 'iconId', 'svgPath', 'connectedArrows', 'startShape', 'endShape', 'startAnchor', 'endAnchor', 'listType', 'listNumber'];

    // Override toObject for all shape types to include custom properties
    const originalToObject = fabric.Object.prototype.toObject;
    fabric.Object.prototype.toObject = function(propertiesToInclude) {
        return originalToObject.call(this, (propertiesToInclude || []).concat(customProperties));
    };

    // Also override for Group
    if (fabric.Group) {
        const originalGroupToObject = fabric.Group.prototype.toObject;
        fabric.Group.prototype.toObject = function(propertiesToInclude) {
            return originalGroupToObject.call(this, (propertiesToInclude || []).concat(customProperties));
        };
    }
})();

// ============================================================================
// Canvas State Variables
// ============================================================================
let canvas;
let currentTool = 'select';
let isDrawing = false;
let startX, startY;
let currentShape = null;
let zoomLevel = 1;
let isPanning = false;
let lastPosX, lastPosY;
let currentShareUrl = '';

// Arrow connection management
let connections = []; // { arrow: fabricObject, fromShape: fabricObject, toShape: fabricObject, fromAnchor: string, toAnchor: string }
let arrowStartShape = null;
let arrowStartAnchor = null;
let snapDistance = 20; // pixels for magnetic snap
let arrowPreviewLine = null; // Preview line while drawing arrow
let arrowPreviewHead = null; // Preview arrow head

// Context menu state
let lastContextMenuX = 0;
let lastContextMenuY = 0;

// ============================================================================
// Toast Notification Functions
// ============================================================================
function showToast(type, title, message, details = null, duration = 5000) {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast-notification ${type}`;

    const icons = {
        success: 'fas fa-check-circle',
        info: 'fas fa-brain',
        warning: 'fas fa-exclamation-triangle'
    };

    let detailsHtml = '';
    if (details && details.length > 0) {
        detailsHtml = `
            <div class="toast-details">
                ${details.map(d => `
                    <div class="memory-item">
                        <span>${d.title}</span>
                        <span style="color: #667eea;">${Math.round(d.similarity * 100)}%</span>
                    </div>
                `).join('')}
            </div>
        `;
    }

    toast.innerHTML = `
        <i class="toast-icon ${icons[type]}"></i>
        <div class="toast-content">
            <div class="toast-title">${title}</div>
            <div class="toast-message">${message}</div>
            ${detailsHtml}
        </div>
        <button class="toast-close" onclick="this.parentElement.remove()">
            <i class="fas fa-times"></i>
        </button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease-in forwards';
        setTimeout(() => toast.remove(), 300);
    }, duration);
}

function showMemoryToast(data) {
    if (data.adoptedCount > 0) {
        showToast('success', '메모리 참고자료 채택', data.message, data.memories, 7000);
    } else if (data.searchedCount > 0) {
        showToast('warning', '메모리 검색 완료', data.message, null, 4000);
    } else {
        showToast('info', '메모리 검색', data.message || '관련 메모리를 찾지 못했습니다.', null, 3000);
    }
}

function showProgress(message) {
    const indicator = document.getElementById('progressIndicator');
    const text = document.getElementById('progressText');
    text.textContent = message;
    indicator.style.display = 'block';
}

function hideProgress() {
    document.getElementById('progressIndicator').style.display = 'none';
}

// ============================================================================
// Canvas Initialization
// ============================================================================
document.addEventListener('DOMContentLoaded', function() {
    initCanvas();
});

function initCanvas() {
    const container = document.getElementById('canvas-container');
    const width = container.clientWidth;
    const height = container.clientHeight - 100; // Leave space for prompt panel

    canvas = new fabric.Canvas('shapeup-canvas', {
        width: width,
        height: height,
        backgroundColor: '#f8f9fa',
        selection: true
    });

    // Mouse wheel zoom
    canvas.on('mouse:wheel', function(opt) {
        const delta = opt.e.deltaY;
        let zoom = canvas.getZoom();
        zoom *= 0.999 ** delta;
        if (zoom > 5) zoom = 5;
        if (zoom < 0.1) zoom = 0.1;
        canvas.zoomToPoint({ x: opt.e.offsetX, y: opt.e.offsetY }, zoom);
        zoomLevel = zoom;
        updateZoomDisplay();
        opt.e.preventDefault();
        opt.e.stopPropagation();
    });

    // Pan with middle mouse, alt+drag, or pan tool
    canvas.on('mouse:down', function(opt) {
        if (opt.e.altKey || opt.e.button === 1 || currentTool === 'pan') {
            isPanning = true;
            canvas.selection = false;
            lastPosX = opt.e.clientX;
            lastPosY = opt.e.clientY;
            canvas.defaultCursor = 'grabbing';
        } else if (currentTool === 'select') {
            // Select tool: click to select object
            if (opt.target) {
                canvas.setActiveObject(opt.target);
                canvas.renderAll();
            } else {
                canvas.discardActiveObject();
                canvas.renderAll();
            }
        } else {
            // Drawing tools
            handleDrawStart(opt);
        }
    });

    canvas.on('mouse:move', function(opt) {
        if (isPanning) {
            const vpt = canvas.viewportTransform;
            vpt[4] += opt.e.clientX - lastPosX;
            vpt[5] += opt.e.clientY - lastPosY;
            canvas.requestRenderAll();
            lastPosX = opt.e.clientX;
            lastPosY = opt.e.clientY;
        } else if (isDrawing || svgBoxDrawing) {
            handleDrawMove(opt);
        }
    });

    canvas.on('mouse:up', function(opt) {
        if (isPanning) {
            isPanning = false;
            if (currentTool === 'pan') {
                canvas.defaultCursor = 'grab';
            } else {
                canvas.selection = true;
            }
        }
        if (isDrawing || svgBoxDrawing) {
            handleDrawEnd(opt);
        }
    });

    // Keyboard shortcuts
    document.addEventListener('keydown', function(e) {
        // Ignore if typing in input/textarea
        if (e.target.matches('input, textarea')) return;

        // Delete/Backspace - delete selected
        if (e.key === 'Delete' || e.key === 'Backspace') {
            deleteSelected();
        }

        // Ctrl+C - copy selected
        if ((e.ctrlKey || e.metaKey) && e.key === 'c') {
            copySelected();
            e.preventDefault();
        }

        // Ctrl+V - paste
        if ((e.ctrlKey || e.metaKey) && e.key === 'v') {
            pasteSelected();
            e.preventDefault();
        }
    });

    // Resize handler
    window.addEventListener('resize', function() {
        const container = document.getElementById('canvas-container');
        canvas.setWidth(container.clientWidth);
        canvas.setHeight(container.clientHeight - 100);
        canvas.renderAll();
    });

    // Object moving - update connected arrows
    canvas.on('object:moving', function(opt) {
        updateConnectedArrows(opt.target);
    });

    // Object scaling/rotating - update connected arrows
    canvas.on('object:scaling', function(opt) {
        updateConnectedArrows(opt.target);
    });
    canvas.on('object:rotating', function(opt) {
        updateConnectedArrows(opt.target);
    });

    // Right-click context menu on canvas
    document.getElementById('canvas-container').addEventListener('contextmenu', function(e) {
        e.preventDefault();
        e.stopPropagation();
        showContextMenu(e.clientX, e.clientY);
    });

    // Hide context menu and property popup on click elsewhere
    document.addEventListener('click', function(e) {
        // Don't close property popup when clicking context menu items
        const isContextMenuClick = e.target.closest('.context-menu');

        if (!isContextMenuClick) {
            hideContextMenu();
        }
        if (!e.target.closest('.property-popup') && !isContextMenuClick) {
            hidePropertyPopup();
        }
    });

    // Double-click to edit text inside group
    canvas.on('mouse:dblclick', function(opt) {
        const target = opt.target;
        if (!target) return;

        // If it's a group, find text inside
        if (target.type === 'group' && !target.connectionInfo) {
            const pointer = canvas.getPointer(opt.e);
            const textObj = findTextInGroup(target, pointer);
            if (textObj) {
                editTextInGroup(target, textObj);
            }
        }
    });

    // Update text properties panel on selection
    canvas.on('selection:created', function(opt) {
        const activeObj = opt.selected ? opt.selected[0] : canvas.getActiveObject();
        const textObj = getTextObjectFromSelection(activeObj);
        if (textObj) {
            updateTextPropertiesPanel(textObj);
            document.getElementById('text-properties').style.display = 'block';
        } else {
            document.getElementById('text-properties').style.display = 'none';
        }
    });

    canvas.on('selection:updated', function(opt) {
        const activeObj = opt.selected ? opt.selected[0] : canvas.getActiveObject();
        const textObj = getTextObjectFromSelection(activeObj);
        if (textObj) {
            updateTextPropertiesPanel(textObj);
            document.getElementById('text-properties').style.display = 'block';
        } else {
            document.getElementById('text-properties').style.display = 'none';
        }
    });

    canvas.on('selection:cleared', function() {
        document.getElementById('text-properties').style.display = 'none';
    });

    // Check for PRD wireframe mode
    checkPrdWireframeMode();
    checkEditMode();
}

// ============================================================================
// PRD Wireframe Mode Handler
// ============================================================================
function checkPrdWireframeMode() {
    const urlParams = new URLSearchParams(window.location.search);
    const isPrdWireframe = urlParams.get('prdWireframe') === 'true';

    if (isPrdWireframe) {
        const wireframePrompt = sessionStorage.getItem('prdWireframePrompt');
        if (wireframePrompt) {
            // Clear sessionStorage
            sessionStorage.removeItem('prdWireframePrompt');

            // Clean URL without reloading
            const cleanUrl = window.location.pathname;
            window.history.replaceState({}, document.title, cleanUrl);

            // Open AI Panel and set prompt (user will click Generate manually)
            setTimeout(() => {
                // Open AI Board Generator panel
                openAIPanel();

                // Select Free Board mode
                setTimeout(() => {
                    const freeBoardBtn = document.querySelector('.board-type-btn[data-type="freeboard"]');
                    if (freeBoardBtn) {
                        freeBoardBtn.click();
                    }

                    // Set prompt text
                    setTimeout(() => {
                        const promptInput = document.getElementById('ai-prompt');
                        if (promptInput) {
                            promptInput.value = wireframePrompt;
                            // Focus on the input
                            promptInput.focus();
                        }

                        // Show toast notification (type, title, message)
                        showToast('info', 'PRD 와이어프레임', 'Generate 버튼을 클릭하여 와이어프레임을 생성하세요.');
                    }, 200);
                }, 200);
            }, 300);
        }
    }
}

// ============================================================================
// Edit Mode Handler (from Share page)
// ============================================================================
function checkEditMode() {
    const urlParams = new URLSearchParams(window.location.search);
    const isEditMode = urlParams.get('editMode') === 'true';

    if (isEditMode) {
        const boardDataStr = sessionStorage.getItem('shapeupEditBoardData');
        if (boardDataStr) {
            // Clear sessionStorage
            sessionStorage.removeItem('shapeupEditBoardData');

            // Clean URL without reloading
            const cleanUrl = window.location.pathname;
            window.history.replaceState({}, document.title, cleanUrl);

            // Load board data after canvas is initialized
            setTimeout(() => {
                loadBoardForEditing(boardDataStr);
            }, 300);
        }
    }
}

// Load board data for editing
function loadBoardForEditing(boardDataStr) {
    try {
        const boardData = JSON.parse(boardDataStr);

        // Clear current canvas
        canvas.clear();
        canvas.backgroundColor = '#f8f9fa';

        // Load from JSON
        canvas.loadFromJSON(boardData, function() {
            // Enable all objects for editing (opposite of Share page read-only mode)
            canvas.forEachObject(function(obj) {
                obj.selectable = true;
                obj.evented = true;
                obj.setCoords();
            });

            canvas.renderAll();

            // Show success toast
            showToast('success', '보드 로드 완료', '공유된 보드를 편집할 수 있습니다. 변경 사항은 자동 저장되지 않습니다.');

            // Switch to select tool
            setTool('select');
        });
    } catch (e) {
        console.error('Error loading board for editing:', e);
        showToast('warning', '로드 실패', '보드 데이터를 불러오는 데 실패했습니다.');
    }
}

// ============================================================================
// Tool Selection
// ============================================================================
function setTool(tool) {
    currentTool = tool;
    // Update tool panel buttons
    document.querySelectorAll('.tool-panel .tool-btn').forEach(btn => btn.classList.remove('active'));
    const toolBtn = document.getElementById('tool-' + tool);
    if (toolBtn) toolBtn.classList.add('active');

    // Update canvas controls pan button
    const panBtn = document.getElementById('tool-pan');
    if (panBtn) {
        if (tool === 'pan') {
            panBtn.classList.add('active');
        } else {
            panBtn.classList.remove('active');
        }
    }

    canvas.isDrawingMode = false;

    // Pan tool: disable selection, enable drag panning
    if (tool === 'pan') {
        canvas.selection = false;
        canvas.defaultCursor = 'grab';
        canvas.hoverCursor = 'grab';
        // Disable object selection during pan
        canvas.forEachObject(function(obj) {
            obj.selectable = false;
            obj.evented = false;
        });
    } else if (tool === 'select') {
        canvas.selection = true;
        canvas.defaultCursor = 'default';
        canvas.hoverCursor = 'move';
        // Enable object selection
        canvas.forEachObject(function(obj) {
            obj.selectable = true;
            obj.evented = true;
            obj.setCoords();
        });
        canvas.renderAll();
    } else {
        // Drawing tools
        canvas.selection = false;
        canvas.defaultCursor = 'crosshair';
        canvas.hoverCursor = 'crosshair';
        // Keep objects non-selectable during drawing
        canvas.forEachObject(function(obj) {
            obj.selectable = false;
            obj.evented = false;
        });
    }
}

// ============================================================================
// AI Panel Functions
// ============================================================================
function toggleAIPanel() {
    const panel = document.getElementById('ai-prompt-panel');
    panel.classList.toggle('show');
}

function closeAIPanel() {
    const panel = document.getElementById('ai-prompt-panel');
    panel.classList.remove('show');
}

function openAIPanel() {
    const panel = document.getElementById('ai-prompt-panel');
    panel.classList.add('show');
}

// ============================================================================
// Drawing Functions
// ============================================================================
function handleDrawStart(opt) {
    if (currentTool === 'select') return;

    // Handle SVG box tool separately
    if (currentTool === 'svgbox') {
        startSvgBoxDraw(opt);
        return;
    }

    isDrawing = true;
    const pointer = canvas.getPointer(opt.e);
    startX = pointer.x;
    startY = pointer.y;

    const fillColor = document.getElementById('fillColor').value;
    const strokeColor = document.getElementById('strokeColor').value;
    const strokeWidth = parseInt(document.getElementById('strokeWidth').value);

    switch (currentTool) {
        case 'rect':
            currentShape = new fabric.Rect({
                left: startX,
                top: startY,
                width: 0,
                height: 0,
                fill: fillColor,
                stroke: strokeColor,
                strokeWidth: strokeWidth,
                rx: 8,
                ry: 8
            });
            break;
        case 'circle':
            currentShape = new fabric.Circle({
                left: startX,
                top: startY,
                radius: 0,
                fill: fillColor,
                stroke: strokeColor,
                strokeWidth: strokeWidth
            });
            break;
        case 'line':
            currentShape = new fabric.Line([startX, startY, startX, startY], {
                stroke: strokeColor,
                strokeWidth: strokeWidth
            });
            break;
        case 'arrow':
            // Arrow: detect start shape for magnetic snap
            const snapResult = findNearestShapeAnchor(pointer.x, pointer.y);
            if (snapResult) {
                arrowStartShape = snapResult.shape;
                arrowStartAnchor = snapResult.anchor;
                startX = snapResult.point.x;
                startY = snapResult.point.y;
            } else {
                arrowStartShape = null;
                arrowStartAnchor = null;
            }
            // Create preview line and arrow head
            arrowPreviewLine = new fabric.Line([startX, startY, startX, startY], {
                stroke: strokeColor,
                strokeWidth: strokeWidth,
                selectable: false,
                evented: false,
                strokeDashArray: [5, 5] // Dashed line for preview
            });
            arrowPreviewHead = new fabric.Triangle({
                left: startX,
                top: startY,
                width: 15,
                height: 15,
                fill: strokeColor,
                angle: 0,
                originX: 'center',
                originY: 'center',
                selectable: false,
                evented: false
            });
            canvas.add(arrowPreviewLine);
            canvas.add(arrowPreviewHead);
            break;
        case 'text':
            currentShape = new fabric.IText('', {
                left: startX,
                top: startY,
                fontSize: 16,
                fill: strokeColor
            });
            canvas.add(currentShape);
            canvas.setActiveObject(currentShape);
            currentShape.enterEditing();

            // Add list continuation handler
            addListKeyHandler(currentShape);

            // Remove text object if empty when editing ends
            currentShape.on('editing:exited', function() {
                if (this.text.trim() === '') {
                    canvas.remove(this);
                    canvas.renderAll();
                }
            });

            isDrawing = false;
            setTool('select');
            return;
    }

    if (currentShape) {
        canvas.add(currentShape);
    }
}

function handleDrawMove(opt) {
    // Handle SVG box drawing
    if (currentTool === 'svgbox' && svgBoxDrawing) {
        updateSvgBoxDraw(opt);
        return;
    }

    if (!isDrawing) return;

    const pointer = canvas.getPointer(opt.e);

    switch (currentTool) {
        case 'rect':
            if (!currentShape) return;
            const width = pointer.x - startX;
            const height = pointer.y - startY;
            currentShape.set({
                width: Math.abs(width),
                height: Math.abs(height),
                left: width > 0 ? startX : pointer.x,
                top: height > 0 ? startY : pointer.y
            });
            break;
        case 'circle':
            if (!currentShape) return;
            const radius = Math.sqrt(Math.pow(pointer.x - startX, 2) + Math.pow(pointer.y - startY, 2)) / 2;
            currentShape.set({ radius: radius });
            break;
        case 'line':
            if (!currentShape) return;
            currentShape.set({ x2: pointer.x, y2: pointer.y });
            break;
        case 'arrow':
            // Update arrow preview
            if (arrowPreviewLine && arrowPreviewHead) {
                let endX = pointer.x;
                let endY = pointer.y;

                // Check for snap to shape
                const snapResult = findNearestShapeAnchor(pointer.x, pointer.y);
                if (snapResult) {
                    endX = snapResult.point.x;
                    endY = snapResult.point.y;
                }

                // Update preview line
                arrowPreviewLine.set({ x2: endX, y2: endY });

                // Update preview arrow head position and angle
                const angle = Math.atan2(endY - startY, endX - startX);
                arrowPreviewHead.set({
                    left: endX,
                    top: endY,
                    angle: (angle * 180 / Math.PI) + 90
                });
            }
            break;
    }

    canvas.renderAll();
}

function handleDrawEnd(opt) {
    // Handle SVG box drawing end
    if (currentTool === 'svgbox' && svgBoxDrawing) {
        endSvgBoxDraw(opt);
        return;
    }

    if (currentTool === 'arrow') {
        const pointer = canvas.getPointer(opt.e);
        let endX = pointer.x;
        let endY = pointer.y;
        let arrowEndShape = null;
        let arrowEndAnchor = null;

        // Detect end shape for magnetic snap
        const snapResult = findNearestShapeAnchor(pointer.x, pointer.y);
        if (snapResult) {
            arrowEndShape = snapResult.shape;
            arrowEndAnchor = snapResult.anchor;
            endX = snapResult.point.x;
            endY = snapResult.point.y;
        }

        // Remove preview elements
        if (arrowPreviewLine) {
            canvas.remove(arrowPreviewLine);
            arrowPreviewLine = null;
        }
        if (arrowPreviewHead) {
            canvas.remove(arrowPreviewHead);
            arrowPreviewHead = null;
        }

        // Create arrow with connection info
        createArrowWithConnection(startX, startY, endX, endY, arrowStartShape, arrowEndShape, arrowStartAnchor, arrowEndAnchor);

        // Reset arrow state
        arrowStartShape = null;
        arrowStartAnchor = null;
    }

    isDrawing = false;
    currentShape = null;
    setTool('select');
}

// ============================================================================
// Arrow Functions
// ============================================================================
function createArrow(x1, y1, x2, y2) {
    const strokeColor = document.getElementById('strokeColor').value;
    const strokeWidth = parseInt(document.getElementById('strokeWidth').value);

    const angle = Math.atan2(y2 - y1, x2 - x1);
    const headLen = 15;

    const line = new fabric.Line([x1, y1, x2, y2], {
        stroke: strokeColor,
        strokeWidth: strokeWidth
    });

    const head = new fabric.Triangle({
        left: x2,
        top: y2,
        width: headLen,
        height: headLen,
        fill: strokeColor,
        angle: (angle * 180 / Math.PI) + 90,
        originX: 'center',
        originY: 'center'
    });

    const group = new fabric.Group([line, head], {
        selectable: true
    });

    canvas.add(group);
}

// Create arrow with connection tracking
function createArrowWithConnection(x1, y1, x2, y2, fromShape, toShape, fromAnchor, toAnchor) {
    const strokeColor = document.getElementById('strokeColor').value;
    const strokeWidth = parseInt(document.getElementById('strokeWidth').value);

    const angle = Math.atan2(y2 - y1, x2 - x1);
    const headLen = 15;

    const line = new fabric.Line([x1, y1, x2, y2], {
        stroke: strokeColor,
        strokeWidth: strokeWidth
    });

    const head = new fabric.Triangle({
        left: x2,
        top: y2,
        width: headLen,
        height: headLen,
        fill: strokeColor,
        angle: (angle * 180 / Math.PI) + 90,
        originX: 'center',
        originY: 'center'
    });

    const group = new fabric.Group([line, head], {
        selectable: true
    });

    // Store connection info on the arrow group
    group.connectionInfo = {
        fromShape: fromShape,
        toShape: toShape,
        fromAnchor: fromAnchor,
        toAnchor: toAnchor
    };

    canvas.add(group);

    // Track the connection if shapes are involved
    if (fromShape || toShape) {
        connections.push({
            arrow: group,
            fromShape: fromShape,
            toShape: toShape,
            fromAnchor: fromAnchor,
            toAnchor: toAnchor
        });
    }
}

// Find the nearest shape and its anchor point for magnetic snap
function findNearestShapeAnchor(x, y) {
    const objects = canvas.getObjects();
    let nearestResult = null;
    let minDistance = Infinity;

    for (const obj of objects) {
        // Skip arrows (groups with line+triangle)
        if (obj.type === 'group' && obj.connectionInfo) continue;

        // Skip arrow preview objects
        if (obj === arrowPreviewLine || obj === arrowPreviewHead) continue;

        // Skip non-selectable objects (lines, triangles that are part of arrows)
        if (obj.type === 'line' && !obj.selectable) continue;
        if (obj.type === 'triangle' && !obj.selectable) continue;

        // Check if point is inside the shape
        const isInside = isPointInsideShape(obj, x, y);
        const anchors = getShapeAnchors(obj);

        for (const anchorName in anchors) {
            if (anchorName === 'center') continue; // Skip center anchor for edge connections

            const anchor = anchors[anchorName];
            const distance = Math.sqrt(Math.pow(x - anchor.x, 2) + Math.pow(y - anchor.y, 2));

            // If inside shape, always snap to nearest anchor of this shape
            if (isInside) {
                if (distance < minDistance) {
                    minDistance = distance;
                    nearestResult = {
                        shape: obj,
                        anchor: anchorName,
                        point: anchor
                    };
                }
            } else if (distance < snapDistance && distance < minDistance) {
                // Outside shape, use snapDistance threshold
                minDistance = distance;
                nearestResult = {
                    shape: obj,
                    anchor: anchorName,
                    point: anchor
                };
            }
        }
    }

    return nearestResult;
}

// Check if point is inside a shape's bounding box
function isPointInsideShape(obj, x, y) {
    const bound = obj.getBoundingRect(true, true);
    return x >= bound.left && x <= bound.left + bound.width &&
           y >= bound.top && y <= bound.top + bound.height;
}

// Get anchor points for a shape (center of each edge)
function getShapeAnchors(obj) {
    const bound = obj.getBoundingRect(true, true);
    const centerX = bound.left + bound.width / 2;
    const centerY = bound.top + bound.height / 2;

    return {
        top: { x: centerX, y: bound.top },
        bottom: { x: centerX, y: bound.top + bound.height },
        left: { x: bound.left, y: centerY },
        right: { x: bound.left + bound.width, y: centerY },
        center: { x: centerX, y: centerY }
    };
}

// Update all arrows connected to a moving shape
function updateConnectedArrows(movedShape) {
    if (!movedShape) return;

    connections.forEach(conn => {
        if (conn.fromShape === movedShape || conn.toShape === movedShape) {
            // Get updated anchor positions
            let x1, y1, x2, y2;

            if (conn.fromShape) {
                const fromAnchors = getShapeAnchors(conn.fromShape);
                const fromPoint = fromAnchors[conn.fromAnchor] || fromAnchors.right;
                x1 = fromPoint.x;
                y1 = fromPoint.y;
            } else {
                // Keep original start position from arrow
                const arrow = conn.arrow;
                if (arrow && arrow._objects && arrow._objects[0]) {
                    x1 = arrow._objects[0].x1 + arrow.left + arrow.width / 2;
                    y1 = arrow._objects[0].y1 + arrow.top + arrow.height / 2;
                }
            }

            if (conn.toShape) {
                const toAnchors = getShapeAnchors(conn.toShape);
                const toPoint = toAnchors[conn.toAnchor] || toAnchors.left;
                x2 = toPoint.x;
                y2 = toPoint.y;
            } else {
                // Keep original end position from arrow
                const arrow = conn.arrow;
                if (arrow && arrow._objects && arrow._objects[0]) {
                    x2 = arrow._objects[0].x2 + arrow.left + arrow.width / 2;
                    y2 = arrow._objects[0].y2 + arrow.top + arrow.height / 2;
                }
            }

            // Update the arrow
            updateArrowPosition(conn.arrow, x1, y1, x2, y2);
        }
    });

    canvas.renderAll();
}

// Update arrow line and head position
function updateArrowPosition(arrow, x1, y1, x2, y2) {
    if (!arrow || !arrow._objects || arrow._objects.length < 2) return;

    const line = arrow._objects[0];
    const head = arrow._objects[1];

    // Ungroup temporarily to update
    const items = arrow._objects;
    arrow._restoreObjectsState();
    canvas.remove(arrow);

    // Update line
    line.set({ x1: x1, y1: y1, x2: x2, y2: y2 });
    line.setCoords();

    // Update arrow head
    const angle = Math.atan2(y2 - y1, x2 - x1);
    head.set({
        left: x2,
        top: y2,
        angle: (angle * 180 / Math.PI) + 90
    });
    head.setCoords();

    // Re-group
    const newGroup = new fabric.Group([line, head], {
        selectable: true
    });
    newGroup.connectionInfo = arrow.connectionInfo;

    canvas.add(newGroup);

    // Update connection reference
    const connIndex = connections.findIndex(c => c.arrow === arrow);
    if (connIndex >= 0) {
        connections[connIndex].arrow = newGroup;
    }
}

// ============================================================================
// Delete Function
// ============================================================================
function deleteSelected() {
    const activeObjects = canvas.getActiveObjects();
    if (activeObjects.length) {
        activeObjects.forEach(obj => {
            // Remove from connections if this is an arrow
            const connIndex = connections.findIndex(c => c.arrow === obj);
            if (connIndex >= 0) {
                connections.splice(connIndex, 1);
            }

            // Remove connections that reference this shape
            connections = connections.filter(c => c.fromShape !== obj && c.toShape !== obj);

            canvas.remove(obj);
        });
        canvas.discardActiveObject();
        canvas.renderAll();
    }
    hideContextMenu();
}

// ============================================================================
// Copy/Paste Functions
// ============================================================================
let clipboard = null;

function copySelected() {
    const activeObject = canvas.getActiveObject();
    if (!activeObject) return;

    // Clone the active object (handles groups, multiple selections)
    activeObject.clone(function(cloned) {
        clipboard = cloned;
    });
}

function pasteSelected() {
    if (!clipboard) return;

    clipboard.clone(function(clonedObj) {
        canvas.discardActiveObject();

        // Offset to the right to avoid overlapping
        const offsetX = 30;
        const offsetY = 30;

        clonedObj.set({
            left: clonedObj.left + offsetX,
            top: clonedObj.top + offsetY,
            evented: true,
        });

        if (clonedObj.type === 'activeSelection') {
            // Multiple objects selected - need to handle each
            clonedObj.canvas = canvas;
            clonedObj.forEachObject(function(obj) {
                canvas.add(obj);
                // Make sure object is selectable and has correct coords
                obj.selectable = true;
                obj.evented = true;
                obj.setCoords();
            });
            // Set them as active selection
            canvas.setActiveObject(clonedObj);
        } else {
            // Single object or group
            canvas.add(clonedObj);
            clonedObj.selectable = true;
            clonedObj.evented = true;
            clonedObj.setCoords();
            canvas.setActiveObject(clonedObj);
        }

        // Update clipboard position for next paste (stack effect)
        clipboard.set({
            left: clipboard.left + offsetX,
            top: clipboard.top + offsetY,
        });

        canvas.renderAll();
    });
}

// ============================================================================
// Context Menu Functions
// ============================================================================
function showContextMenu(x, y) {
    lastContextMenuX = x;
    lastContextMenuY = y;

    const menu = document.getElementById('context-menu');
    const activeObjects = canvas.getActiveObjects();
    const activeObject = canvas.getActiveObject();

    // Update menu items based on selection
    const propertiesItem = document.getElementById('ctx-properties');
    const groupItem = document.getElementById('ctx-group');
    const ungroupItem = document.getElementById('ctx-ungroup');
    const deleteItem = document.getElementById('ctx-delete');
    const bringFrontItem = document.getElementById('ctx-bring-front');
    const sendBackItem = document.getElementById('ctx-send-back');

    // Properties: enabled when any object selected
    if (activeObjects.length > 0) {
        propertiesItem.classList.remove('disabled');
    } else {
        propertiesItem.classList.add('disabled');
    }

    // Group: enabled when 2+ objects selected
    if (activeObjects.length >= 2) {
        groupItem.classList.remove('disabled');
    } else {
        groupItem.classList.add('disabled');
    }

    // Ungroup: enabled when a group is selected (not arrow group)
    if (activeObject && activeObject.type === 'group' && !activeObject.connectionInfo) {
        ungroupItem.classList.remove('disabled');
    } else {
        ungroupItem.classList.add('disabled');
    }

    // Delete, Bring to Front, Send to Back: enabled when any object selected
    if (activeObjects.length > 0) {
        deleteItem.classList.remove('disabled');
        bringFrontItem.classList.remove('disabled');
        sendBackItem.classList.remove('disabled');
    } else {
        deleteItem.classList.add('disabled');
        bringFrontItem.classList.add('disabled');
        sendBackItem.classList.add('disabled');
    }

    // Position menu
    menu.style.left = x + 'px';
    menu.style.top = y + 'px';
    menu.classList.add('show');
}

function hideContextMenu() {
    const menu = document.getElementById('context-menu');
    menu.classList.remove('show');
}

// ============================================================================
// Property Popup Functions
// ============================================================================
function showPropertyPopup(e) {
    if (e) {
        e.stopPropagation();
        e.preventDefault();
    }

    const propsItem = document.getElementById('ctx-properties');
    if (propsItem && propsItem.classList.contains('disabled')) {
        hideContextMenu();
        return;
    }

    const popup = document.getElementById('property-popup');
    if (!popup) {
        console.error('Property popup element not found');
        return;
    }

    const activeObj = canvas.getActiveObject();
    if (!activeObj) {
        hideContextMenu();
        return;
    }

    const textObj = getTextObjectFromSelection(activeObj);

    // Show/hide sections based on selection type
    const shapeProps = document.getElementById('popup-shape-props');
    const textProps = document.getElementById('popup-text-props');

    // Always show shape properties if object is selected
    if (shapeProps) shapeProps.style.display = 'block';

    // Show text properties only if text object is selected or inside group
    if (textProps) {
        if (textObj) {
            textProps.style.display = 'block';
            updatePopupTextProperties(textObj);
        } else {
            textProps.style.display = 'none';
        }
    }

    // Update shape properties from selected object
    updatePopupShapeProperties(activeObj);

    // Position popup at context menu location
    let popupX = lastContextMenuX;
    let popupY = lastContextMenuY;

    // Ensure minimum position
    popupX = Math.max(10, popupX);
    popupY = Math.max(10, popupY);

    // Adjust position if popup would go off screen
    const popupWidth = 500;
    const popupHeight = textObj ? 220 : 140;
    if (popupX + popupWidth > window.innerWidth) {
        popupX = window.innerWidth - popupWidth - 20;
    }
    if (popupY + popupHeight > window.innerHeight) {
        popupY = window.innerHeight - popupHeight - 20;
    }

    popup.style.left = popupX + 'px';
    popup.style.top = popupY + 'px';
    popup.style.display = 'block'; // Explicitly set display
    popup.classList.add('show');

    // Hide context menu after showing popup
    hideContextMenu();
}

function hidePropertyPopup() {
    const popup = document.getElementById('property-popup');
    if (popup) {
        popup.classList.remove('show');
        popup.style.display = 'none';
    }
}

function updatePopupShapeProperties(obj) {
    // Fill color
    const fillColor = document.getElementById('popup-fillColor');
    if (obj.fill && typeof obj.fill === 'string' && obj.fill.startsWith('#')) {
        fillColor.value = obj.fill;
    }

    // Stroke color
    const strokeColor = document.getElementById('popup-strokeColor');
    if (obj.stroke && typeof obj.stroke === 'string' && obj.stroke.startsWith('#')) {
        strokeColor.value = obj.stroke;
    }

    // Stroke width
    const strokeWidth = document.getElementById('popup-strokeWidth');
    if (obj.strokeWidth) {
        strokeWidth.value = obj.strokeWidth;
    }
}

function updatePopupTextProperties(textObj) {
    // Text color
    const textColorInput = document.getElementById('popup-textColor');
    if (textObj.fill && typeof textObj.fill === 'string') {
        textColorInput.value = textObj.fill.startsWith('#') ? textObj.fill : '#333333';
    }

    // Text size
    const textSizeSelect = document.getElementById('popup-textSize');
    if (textObj.fontSize) {
        const size = textObj.fontSize;
        if (size >= 32) textSizeSelect.value = '32';
        else if (size >= 24) textSizeSelect.value = '24';
        else if (size >= 18) textSizeSelect.value = '18';
        else textSizeSelect.value = '14';
    }

    // Alignment buttons
    document.querySelectorAll('.popup-text-align-btn').forEach(btn => btn.classList.remove('active'));
    const align = textObj.textAlign || 'center';
    document.querySelectorAll('.popup-text-align-btn').forEach(btn => {
        if (btn.getAttribute('onclick').includes(`'${align}'`)) {
            btn.classList.add('active');
        }
    });

    // Style buttons
    document.getElementById('popup-btn-bold').classList.toggle('active', textObj.fontWeight === 'bold');
    document.getElementById('popup-btn-italic').classList.toggle('active', textObj.fontStyle === 'italic');
    document.getElementById('popup-btn-underline').classList.toggle('active', textObj.underline === true);
}

// Popup-specific update functions
function updateSelectedFillFromPopup() {
    const color = document.getElementById('popup-fillColor').value;
    const activeObj = canvas.getActiveObject();
    if (activeObj) {
        activeObj.set('fill', color);
        // Also update main panel
        document.getElementById('fillColor').value = color;
        canvas.renderAll();
    }
}

function updateSelectedStrokeFromPopup() {
    const color = document.getElementById('popup-strokeColor').value;
    const activeObj = canvas.getActiveObject();
    if (activeObj) {
        activeObj.set('stroke', color);
        // Also update main panel
        document.getElementById('strokeColor').value = color;
        canvas.renderAll();
    }
}

function updateSelectedStrokeWidthFromPopup() {
    const width = parseInt(document.getElementById('popup-strokeWidth').value);
    const activeObj = canvas.getActiveObject();
    if (activeObj) {
        activeObj.set('strokeWidth', width);
        // Also update main panel
        document.getElementById('strokeWidth').value = width;
        canvas.renderAll();
    }
}

function updateSelectedTextColorFromPopup() {
    const color = document.getElementById('popup-textColor').value;
    const activeObj = canvas.getActiveObject();
    const textObj = getTextObjectFromSelection(activeObj);
    if (textObj) {
        textObj.set('fill', color);
        // Also update main panel
        document.getElementById('textColor').value = color;
        canvas.renderAll();
    }
}

function updateSelectedTextSizeFromPopup() {
    const size = parseInt(document.getElementById('popup-textSize').value);
    const activeObj = canvas.getActiveObject();
    const textObj = getTextObjectFromSelection(activeObj);
    if (textObj) {
        textObj.set('fontSize', size);
        // Also update main panel
        document.getElementById('textSize').value = size;
        canvas.renderAll();
    }
}

function updateTextAlignFromPopup(align) {
    const activeObj = canvas.getActiveObject();
    const textObj = getTextObjectFromSelection(activeObj);
    if (textObj) {
        textObj.set('textAlign', align);
        canvas.renderAll();
    }
    // Update popup button state
    document.querySelectorAll('.popup-text-align-btn').forEach(btn => btn.classList.remove('active'));
    event.currentTarget.classList.add('active');
    // Update main panel button state
    document.querySelectorAll('.text-align-btn').forEach(btn => btn.classList.remove('active'));
    document.querySelectorAll('.text-align-btn').forEach(btn => {
        if (btn.getAttribute('onclick').includes(`'${align}'`)) {
            btn.classList.add('active');
        }
    });
}

function toggleTextStyleFromPopup(style) {
    const activeObj = canvas.getActiveObject();
    const textObj = getTextObjectFromSelection(activeObj);
    if (textObj) {
        switch (style) {
            case 'bold':
                const currentWeight = textObj.get('fontWeight');
                textObj.set('fontWeight', currentWeight === 'bold' ? 'normal' : 'bold');
                document.getElementById('popup-btn-bold').classList.toggle('active', textObj.get('fontWeight') === 'bold');
                document.getElementById('btn-bold').classList.toggle('active', textObj.get('fontWeight') === 'bold');
                break;
            case 'italic':
                const currentStyle = textObj.get('fontStyle');
                textObj.set('fontStyle', currentStyle === 'italic' ? 'normal' : 'italic');
                document.getElementById('popup-btn-italic').classList.toggle('active', textObj.get('fontStyle') === 'italic');
                document.getElementById('btn-italic').classList.toggle('active', textObj.get('fontStyle') === 'italic');
                break;
            case 'underline':
                const currentUnderline = textObj.get('underline');
                textObj.set('underline', !currentUnderline);
                document.getElementById('popup-btn-underline').classList.toggle('active', textObj.get('underline'));
                document.getElementById('btn-underline').classList.toggle('active', textObj.get('underline'));
                break;
        }
        canvas.renderAll();
    }
}

function insertListFromPopup(type) {
    insertList(type);
}

// ============================================================================
// Group Functions
// ============================================================================
function groupSelected() {
    const activeObjects = canvas.getActiveObjects();
    if (activeObjects.length < 2) return;

    // Check if disabled
    if (document.getElementById('ctx-group').classList.contains('disabled')) return;

    canvas.discardActiveObject();

    const group = new fabric.Group(activeObjects, {
        selectable: true,
        subTargetCheck: true, // Enable sub-target checking for text editing
        interactive: true
    });

    // Mark as user group (not arrow)
    group.isUserGroup = true;

    // Remove individual objects and add group
    activeObjects.forEach(obj => canvas.remove(obj));
    canvas.add(group);
    canvas.setActiveObject(group);
    canvas.renderAll();

    hideContextMenu();
}

function ungroupSelected() {
    const activeObject = canvas.getActiveObject();
    if (!activeObject || activeObject.type !== 'group' || activeObject.connectionInfo) return;

    // Check if disabled
    if (document.getElementById('ctx-ungroup').classList.contains('disabled')) return;

    const items = activeObject._objects;
    activeObject._restoreObjectsState();
    canvas.remove(activeObject);

    items.forEach(item => {
        item.selectable = true;
        item.evented = true;
        canvas.add(item);
    });

    canvas.discardActiveObject();
    canvas.renderAll();

    hideContextMenu();
}

function bringToFront() {
    const activeObject = canvas.getActiveObject();
    if (!activeObject) return;

    if (document.getElementById('ctx-bring-front').classList.contains('disabled')) return;

    canvas.bringToFront(activeObject);
    canvas.renderAll();
    hideContextMenu();
}

function sendToBack() {
    const activeObject = canvas.getActiveObject();
    if (!activeObject) return;

    if (document.getElementById('ctx-send-back').classList.contains('disabled')) return;

    canvas.sendToBack(activeObject);
    canvas.renderAll();
    hideContextMenu();
}

// ============================================================================
// Text Editing in Groups
// ============================================================================
function findTextInGroup(group, pointer) {
    if (!group._objects) return null;

    // Get group's transform
    const groupCenter = group.getCenterPoint();
    const groupAngle = group.angle || 0;
    const groupScaleX = group.scaleX || 1;
    const groupScaleY = group.scaleY || 1;

    // Transform pointer to group's local coordinate
    const cos = Math.cos(-groupAngle * Math.PI / 180);
    const sin = Math.sin(-groupAngle * Math.PI / 180);
    const dx = pointer.x - groupCenter.x;
    const dy = pointer.y - groupCenter.y;
    const localX = (dx * cos - dy * sin) / groupScaleX;
    const localY = (dx * sin + dy * cos) / groupScaleY;

    // Collect all text objects in the group (including nested groups)
    const textObjects = [];
    function collectTextObjects(objects) {
        for (const obj of objects) {
            if (obj.type === 'i-text' || obj.type === 'text' || obj.type === 'textbox') {
                textObjects.push(obj);
            } else if (obj.type === 'group' && obj._objects) {
                collectTextObjects(obj._objects);
            }
        }
    }
    collectTextObjects(group._objects);

    // Find text object that contains the point (with expanded hit area)
    const hitMargin = 10; // Extra margin for easier selection
    for (const obj of textObjects) {
        const objBound = obj.getBoundingRect(false, true);
        const objLeft = obj.left - objBound.width / 2 - hitMargin;
        const objTop = obj.top - objBound.height / 2 - hitMargin;
        const objRight = obj.left + objBound.width / 2 + hitMargin;
        const objBottom = obj.top + objBound.height / 2 + hitMargin;

        if (localX >= objLeft && localX <= objRight && localY >= objTop && localY <= objBottom) {
            return obj;
        }
    }

    // Fallback: if no text found at pointer, return closest text object
    if (textObjects.length > 0) {
        let closestText = null;
        let minDistance = Infinity;

        for (const obj of textObjects) {
            const dist = Math.sqrt(Math.pow(localX - obj.left, 2) + Math.pow(localY - obj.top, 2));
            if (dist < minDistance) {
                minDistance = dist;
                closestText = obj;
            }
        }

        // Only return if within reasonable distance (150px)
        if (minDistance < 150) {
            return closestText;
        }
    }

    return null;
}

function editTextInGroup(group, textObj) {
    // Store group reference and position
    const groupLeft = group.left;
    const groupTop = group.top;
    const groupAngle = group.angle;
    const groupScaleX = group.scaleX;
    const groupScaleY = group.scaleY;

    // Get all objects from group
    const items = group._objects.slice();
    const textIndex = items.indexOf(textObj);

    // Ungroup temporarily
    group._restoreObjectsState();
    canvas.remove(group);

    // Add all items back
    items.forEach(item => {
        item.selectable = (item === textObj);
        item.evented = (item === textObj);
        canvas.add(item);
    });

    // Select and edit the text
    canvas.setActiveObject(textObj);
    if (textObj.type === 'i-text' || textObj.type === 'textbox') {
        // Add list continuation handler
        addListKeyHandler(textObj);
        textObj.enterEditing();
        textObj.selectAll();
    }

    // Re-group when editing ends
    textObj.on('editing:exited', function onEditEnd() {
        textObj.off('editing:exited', onEditEnd);

        // Remove all items
        items.forEach(item => canvas.remove(item));

        // Re-create group
        const newGroup = new fabric.Group(items, {
            left: groupLeft,
            top: groupTop,
            angle: groupAngle,
            scaleX: groupScaleX,
            scaleY: groupScaleY,
            selectable: true,
            subTargetCheck: true,
            interactive: true
        });
        newGroup.isUserGroup = true;

        canvas.add(newGroup);
        canvas.setActiveObject(newGroup);
        canvas.renderAll();
    });

    canvas.renderAll();
}

// ============================================================================
// Property Update Functions
// ============================================================================
function updateSelectedFill() {
    const color = document.getElementById('fillColor').value;
    const activeObj = canvas.getActiveObject();
    if (activeObj) {
        activeObj.set('fill', color);
        canvas.renderAll();
    }
}

function updateSelectedStroke() {
    const color = document.getElementById('strokeColor').value;
    const activeObj = canvas.getActiveObject();
    if (activeObj) {
        activeObj.set('stroke', color);
        canvas.renderAll();
    }
}

function updateSelectedStrokeWidth() {
    const width = parseInt(document.getElementById('strokeWidth').value);
    const activeObj = canvas.getActiveObject();
    if (activeObj) {
        activeObj.set('strokeWidth', width);
        canvas.renderAll();
    }
}

// ============================================================================
// Text Property Functions
// ============================================================================
function getTextObjectFromSelection(activeObj) {
    if (!activeObj) return null;

    // Direct text object
    if (activeObj.type === 'i-text' || activeObj.type === 'text' || activeObj.type === 'textbox') {
        return activeObj;
    }

    // Text inside group (recursively search nested groups)
    if (activeObj.type === 'group') {
        function findTextRecursive(objects) {
            for (const obj of objects) {
                if (obj.type === 'i-text' || obj.type === 'text' || obj.type === 'textbox') {
                    return obj;
                }
                if (obj.type === 'group' && obj._objects) {
                    const nested = findTextRecursive(obj._objects);
                    if (nested) return nested;
                }
            }
            return null;
        }
        return findTextRecursive(activeObj.getObjects());
    }

    return null;
}

function updateSelectedTextColor() {
    const color = document.getElementById('textColor').value;
    const activeObj = canvas.getActiveObject();
    const textObj = getTextObjectFromSelection(activeObj);

    if (textObj) {
        textObj.set('fill', color);
        canvas.renderAll();
    }
}

function updateSelectedTextSize() {
    const size = parseInt(document.getElementById('textSize').value);
    const activeObj = canvas.getActiveObject();
    const textObj = getTextObjectFromSelection(activeObj);

    if (textObj) {
        textObj.set('fontSize', size);
        canvas.renderAll();
    }
}

function updateTextAlign(align) {
    const activeObj = canvas.getActiveObject();
    const textObj = getTextObjectFromSelection(activeObj);

    if (textObj) {
        textObj.set('textAlign', align);
        canvas.renderAll();
    }

    // Update button active state
    document.querySelectorAll('.text-align-btn').forEach(btn => btn.classList.remove('active'));
    event.currentTarget.classList.add('active');
}

function toggleTextStyle(style) {
    const activeObj = canvas.getActiveObject();
    const textObj = getTextObjectFromSelection(activeObj);

    if (textObj) {
        switch (style) {
            case 'bold':
                const currentWeight = textObj.get('fontWeight');
                textObj.set('fontWeight', currentWeight === 'bold' ? 'normal' : 'bold');
                document.getElementById('btn-bold').classList.toggle('active', textObj.get('fontWeight') === 'bold');
                break;
            case 'italic':
                const currentStyle = textObj.get('fontStyle');
                textObj.set('fontStyle', currentStyle === 'italic' ? 'normal' : 'italic');
                document.getElementById('btn-italic').classList.toggle('active', textObj.get('fontStyle') === 'italic');
                break;
            case 'underline':
                const currentUnderline = textObj.get('underline');
                textObj.set('underline', !currentUnderline);
                document.getElementById('btn-underline').classList.toggle('active', textObj.get('underline'));
                break;
        }
        canvas.renderAll();
    }
}

function insertList(type) {
    const activeObj = canvas.getActiveObject();
    const textObj = getTextObjectFromSelection(activeObj);

    if (textObj) {
        const currentText = textObj.get('text') || '';
        const lines = currentText.split('\n');
        let newLines;

        if (type === 'ordered') {
            // Check if already numbered list
            const isNumbered = lines.every((line, i) => line.startsWith(`${i + 1}. `) || line.trim() === '');
            if (isNumbered) {
                // Remove numbering
                newLines = lines.map(line => line.replace(/^\d+\.\s*/, ''));
            } else {
                // Add numbering
                let num = 1;
                newLines = lines.map(line => {
                    if (line.trim() === '') return line;
                    // Remove bullet if exists
                    line = line.replace(/^[•\-]\s*/, '');
                    return `${num++}. ${line}`;
                });
            }
        } else {
            // Unordered (bullet) list
            const isBulleted = lines.every(line => line.startsWith('• ') || line.startsWith('- ') || line.trim() === '');
            if (isBulleted) {
                // Remove bullets
                newLines = lines.map(line => line.replace(/^[•\-]\s*/, ''));
            } else {
                // Add bullets
                newLines = lines.map(line => {
                    if (line.trim() === '') return line;
                    // Remove numbering if exists
                    line = line.replace(/^\d+\.\s*/, '');
                    return `• ${line}`;
                });
            }
        }

        textObj.set('text', newLines.join('\n'));
        canvas.renderAll();
    }
}

// Add keyboard handler for list continuation
function addListKeyHandler(textObj) {
    if (!textObj || textObj._listKeyHandlerAdded) return;
    textObj._listKeyHandlerAdded = true;

    textObj.on('changed', function() {
        const text = this.text || '';
        const lines = text.split('\n');
        const cursorPos = this.selectionStart;

        // Find current line index
        let charCount = 0;
        let currentLineIndex = 0;
        for (let i = 0; i < lines.length; i++) {
            charCount += lines[i].length + 1; // +1 for newline
            if (charCount > cursorPos) {
                currentLineIndex = i;
                break;
            }
        }

        // Check if just pressed Enter (cursor at start of new line after list item)
        if (currentLineIndex > 0 && lines[currentLineIndex] === '') {
            const prevLine = lines[currentLineIndex - 1];

            // Check for bullet list
            if (prevLine.match(/^[•\-]\s*.+$/)) {
                // Previous line has bullet with content, add bullet to new line
                lines[currentLineIndex] = '• ';
                this.text = lines.join('\n');
                this.selectionStart = this.selectionEnd = cursorPos + 2;
                canvas.renderAll();
            }
            // Check for numbered list
            else if (prevLine.match(/^\d+\.\s*.+$/)) {
                // Previous line has number with content, add next number
                const prevNum = parseInt(prevLine.match(/^(\d+)\./)[1]);
                lines[currentLineIndex] = `${prevNum + 1}. `;
                this.text = lines.join('\n');
                this.selectionStart = this.selectionEnd = cursorPos + `${prevNum + 1}. `.length;
                canvas.renderAll();
            }
            // Check for empty bullet/number (consecutive empty lines = release list)
            else if (prevLine.match(/^[•\-]\s*$/) || prevLine.match(/^\d+\.\s*$/)) {
                // Previous line is empty bullet/number, remove it (release list)
                lines[currentLineIndex - 1] = '';
                this.text = lines.join('\n');
                // Adjust cursor position
                const removedChars = prevLine.length;
                this.selectionStart = this.selectionEnd = cursorPos - removedChars;
                canvas.renderAll();
            }
        }
    });
}

function updateTextPropertiesPanel(textObj) {
    if (!textObj) return;

    // Update text color
    const textColorInput = document.getElementById('textColor');
    if (textColorInput && textObj.fill) {
        textColorInput.value = textObj.fill;
    }

    // Update text size
    const textSizeSelect = document.getElementById('textSize');
    if (textSizeSelect && textObj.fontSize) {
        const size = textObj.fontSize;
        if (size >= 32) textSizeSelect.value = '32';
        else if (size >= 24) textSizeSelect.value = '24';
        else if (size >= 18) textSizeSelect.value = '18';
        else textSizeSelect.value = '14';
    }

    // Update alignment buttons
    document.querySelectorAll('.text-align-btn').forEach(btn => btn.classList.remove('active'));
    const align = textObj.textAlign || 'center';
    const alignBtn = document.querySelector(`.text-align-btn[onclick="updateTextAlign('${align}')"]`);
    if (alignBtn) alignBtn.classList.add('active');

    // Update style buttons
    document.getElementById('btn-bold').classList.toggle('active', textObj.fontWeight === 'bold');
    document.getElementById('btn-italic').classList.toggle('active', textObj.fontStyle === 'italic');
    document.getElementById('btn-underline').classList.toggle('active', textObj.underline === true);
}

// ============================================================================
// Zoom Functions
// ============================================================================
function zoomIn() {
    zoomLevel = Math.min(5, zoomLevel * 1.2);
    canvas.setZoom(zoomLevel);
    updateZoomDisplay();
}

function zoomOut() {
    zoomLevel = Math.max(0.1, zoomLevel / 1.2);
    canvas.setZoom(zoomLevel);
    updateZoomDisplay();
}

function resetZoom() {
    zoomLevel = 1;
    canvas.setZoom(1);
    canvas.viewportTransform = [1, 0, 0, 1, 0, 0];
    canvas.renderAll();
    updateZoomDisplay();
}

function updateZoomDisplay() {
    document.getElementById('zoom-level').textContent = Math.round(zoomLevel * 100) + '%';
}

function clearCanvas() {
    if (confirm('Are you sure you want to clear the canvas?')) {
        canvas.clear();
        canvas.backgroundColor = '#f8f9fa';
        canvas.renderAll();
    }
}

// ============================================================================
// Export Functions
// ============================================================================
function exportToPNG() {
    const dataURL = canvas.toDataURL({
        format: 'png',
        quality: 1.0,
        multiplier: 2
    });
    const link = document.createElement('a');
    link.download = 'shapeup-board.png';
    link.href = dataURL;
    link.click();
}

function exportToSVG() {
    const svg = canvas.toSVG();
    const blob = new Blob([svg], { type: 'image/svg+xml' });
    saveAs(blob, 'shapeup-board.svg');
}

// ============================================================================
// SVG Icon Library
// ============================================================================
const svgIcons = {
    'arrow-right': {
        path: 'M5 12h14M12 5l7 7-7 7',
        viewBox: '0 0 24 24',
        name: 'Arrow Right'
    },
    'arrow-left': {
        path: 'M19 12H5M12 19l-7-7 7-7',
        viewBox: '0 0 24 24',
        name: 'Arrow Left'
    },
    'arrow-up': {
        path: 'M12 19V5M5 12l7-7 7 7',
        viewBox: '0 0 24 24',
        name: 'Arrow Up'
    },
    'arrow-down': {
        path: 'M12 5v14M19 12l-7 7-7-7',
        viewBox: '0 0 24 24',
        name: 'Arrow Down'
    },
    'arrow-bidirectional': {
        path: 'M5 12h14M5 12l4-4M5 12l4 4M19 12l-4-4M19 12l-4 4',
        viewBox: '0 0 24 24',
        name: 'Bidirectional'
    },
    'user': {
        path: 'M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
        viewBox: '0 0 24 24',
        name: 'User'
    },
    'users': {
        path: 'M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75',
        viewBox: '0 0 24 24',
        name: 'Users'
    },
    'cloud': {
        path: 'M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z',
        viewBox: '0 0 24 24',
        name: 'Cloud'
    },
    'cloud-upload': {
        path: 'M16 16l-4-4-4 4M12 12v9M20.39 18.39A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 3 16.3',
        viewBox: '0 0 24 24',
        name: 'Cloud Upload'
    },
    'cloud-download': {
        path: 'M8 17l4 4 4-4M12 12v9M20.88 18.09A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 3 16.29',
        viewBox: '0 0 24 24',
        name: 'Cloud Download'
    },
    'server': {
        path: 'M2 4h20v6H2zM2 14h20v6H2zM6 7h.01M6 17h.01',
        viewBox: '0 0 24 24',
        name: 'Server'
    },
    'server-stack': {
        path: 'M2 2h20v5H2zM2 9h20v5H2zM2 16h20v5H2zM6 4.5h.01M6 11.5h.01M6 18.5h.01',
        viewBox: '0 0 24 24',
        name: 'Server Stack'
    },
    'database': {
        path: 'M12 2C6.48 2 2 4.02 2 6.5v11c0 2.48 4.48 4.5 10 4.5s10-2.02 10-4.5v-11c0-2.48-4.48-4.5-10-4.5zM2 12c0 2.48 4.48 4.5 10 4.5s10-2.02 10-4.5M2 6.5c0 2.48 4.48 4.5 10 4.5s10-2.02 10-4.5',
        viewBox: '0 0 24 24',
        name: 'Database'
    },
    'database-alt': {
        path: 'M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4M4 12c0 2.21 3.582 4 8 4s8-1.79 8-4',
        viewBox: '0 0 24 24',
        name: 'Database Alt'
    },
    'monitor': {
        path: 'M2 4h20v12H2zM8 20h8M12 16v4',
        viewBox: '0 0 24 24',
        name: 'Monitor'
    },
    'smartphone': {
        path: 'M5 2h14a1 1 0 0 1 1 1v18a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1zM12 18h.01',
        viewBox: '0 0 24 24',
        name: 'Smartphone'
    },
    'globe': {
        path: 'M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zM2 12h20M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z',
        viewBox: '0 0 24 24',
        name: 'Globe'
    },
    'lock': {
        path: 'M5 11h14v10H5zM8 11V7a4 4 0 1 1 8 0v4M12 15v2',
        viewBox: '0 0 24 24',
        name: 'Lock'
    },
    'api': {
        path: 'M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM17 14v3h-3M17 20v-3h3M14 17h6',
        viewBox: '0 0 24 24',
        name: 'API'
    },
    'gear': {
        path: 'M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z',
        viewBox: '0 0 24 24',
        name: 'Gear'
    },
    'document': {
        path: 'M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8zM14 2v6h6M16 13H8M16 17H8M10 9H8',
        viewBox: '0 0 24 24',
        name: 'Document'
    },
    'folder': {
        path: 'M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z',
        viewBox: '0 0 24 24',
        name: 'Folder'
    }
};

// Get SVG icon list for UI
function getSvgIconList() {
    return Object.keys(svgIcons).map(key => ({
        id: key,
        name: svgIcons[key].name
    }));
}

// ============================================================================
// SVG Box Functions
// ============================================================================

// SVG Templates for quick selection
const svgTemplates = {
    'star': 'M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z',
    'heart': 'M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z',
    'lightning': 'M13 2L3 14h9l-1 8 10-12h-9l1-8z',
    'check': 'M20 6L9 17l-5-5',
    'x': 'M18 6L6 18M6 6l12 12',
    'box': 'M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z',
    'hexagon': 'M21 16.5V7.5L12 2 3 7.5v9L12 22l9-5.5z',
    'diamond': 'M12 2L2 12l10 10 10-10L12 2z',
    'default': 'M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z' // Default: star
};

// Pending SVG box data (waiting for modal input)
let pendingSvgBox = null;

// Create SVG box with custom path content
function createSvgBox(x, y, width, height, pathData = null, strokeColor = null, strokeWidth = null) {
    const sc = strokeColor || document.getElementById('strokeColor').value;
    const sw = strokeWidth || parseInt(document.getElementById('strokeWidth').value);

    // Use provided path or default
    const path = pathData || svgTemplates.default;

    // Validate path
    const isValid = validateSvgPath(path);

    if (isValid) {
        // Create SVG path object
        createSvgPathObject(x, y, width, height, path, sc, sw);
    } else {
        // Create error placeholder (X mark)
        createSvgErrorPlaceholder(x, y, width, height, sc, sw);
    }
}

// Create actual SVG path object on canvas
function createSvgPathObject(x, y, width, height, pathData, strokeColor, strokeWidth) {
    const svgString = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
        <path d="${pathData}" fill="none" stroke="${strokeColor}" stroke-width="${strokeWidth}" stroke-linecap="round" stroke-linejoin="round"/>
    </svg>`;

    fabric.loadSVGFromString(svgString, function(objects, options) {
        if (objects && objects.length > 0) {
            const svgGroup = fabric.util.groupSVGElements(objects, options);

            // Scale to fit the drawn box
            const scaleX = width / 24;
            const scaleY = height / 24;
            const scale = Math.min(scaleX, scaleY) * 0.8; // 80% to add padding

            svgGroup.set({
                left: x + width / 2,
                top: y + height / 2,
                scaleX: scale,
                scaleY: scale,
                originX: 'center',
                originY: 'center',
                objectType: 'svgbox',
                svgPath: pathData
            });

            // Add border frame
            const frame = new fabric.Rect({
                left: x,
                top: y,
                width: width,
                height: height,
                fill: 'transparent',
                stroke: strokeColor,
                strokeWidth: 1,
                strokeDashArray: [4, 4],
                selectable: false,
                evented: false
            });

            // Group path and frame
            const group = new fabric.Group([frame, svgGroup], {
                left: x,
                top: y,
                objectType: 'svgbox',
                svgPath: pathData
            });

            canvas.add(group);
            canvas.setActiveObject(group);
            canvas.renderAll();
        } else {
            createSvgErrorPlaceholder(x, y, width, height, strokeColor, strokeWidth);
        }
    });
}

// Create error placeholder with X mark
function createSvgErrorPlaceholder(x, y, width, height, strokeColor, strokeWidth) {
    const frame = new fabric.Rect({
        left: 0,
        top: 0,
        width: width,
        height: height,
        fill: '#fff5f5',
        stroke: '#dc3545',
        strokeWidth: 2
    });

    const line1 = new fabric.Line([10, 10, width - 10, height - 10], {
        stroke: '#dc3545',
        strokeWidth: 2
    });

    const line2 = new fabric.Line([width - 10, 10, 10, height - 10], {
        stroke: '#dc3545',
        strokeWidth: 2
    });

    const group = new fabric.Group([frame, line1, line2], {
        left: x,
        top: y,
        objectType: 'svgbox-error',
        svgPath: ''
    });

    canvas.add(group);
    canvas.setActiveObject(group);
    canvas.renderAll();
}

// Validate SVG path
function validateSvgPath(pathData) {
    if (!pathData || typeof pathData !== 'string' || pathData.trim() === '') {
        return false;
    }

    // Basic validation: should start with valid command
    const validCommands = /^[MmZzLlHhVvCcSsQqTtAa]/;
    const trimmed = pathData.trim();

    if (!validCommands.test(trimmed)) {
        return false;
    }

    // Try to create a path to validate
    try {
        const testPath = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        testPath.setAttribute('d', trimmed);
        const length = testPath.getTotalLength();
        return !isNaN(length) && length > 0;
    } catch (e) {
        return false;
    }
}

// Open SVG content modal after drawing box
function openSvgContentModal(x, y, width, height) {
    pendingSvgBox = { x, y, width, height };

    // Set default values
    const strokeColor = document.getElementById('strokeColor').value;
    document.getElementById('svg-path-input').value = svgTemplates.default;
    document.getElementById('svg-stroke-color').value = strokeColor;
    document.getElementById('svg-stroke-width').value = '2';

    // Update preview
    updateSvgPreview();

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('svgContentModal'));
    modal.show();

    // Add input listener for live preview
    document.getElementById('svg-path-input').addEventListener('input', updateSvgPreview);
    document.getElementById('svg-stroke-color').addEventListener('input', updateSvgPreview);
    document.getElementById('svg-stroke-width').addEventListener('input', updateSvgPreview);
}

// Update SVG preview in modal
function updateSvgPreview() {
    const pathData = document.getElementById('svg-path-input').value;
    const strokeColor = document.getElementById('svg-stroke-color').value;
    const strokeWidth = document.getElementById('svg-stroke-width').value;

    const previewPath = document.getElementById('svg-preview-path');
    const previewError = document.getElementById('svg-preview-error');
    const previewSvg = document.getElementById('svg-preview');

    if (validateSvgPath(pathData)) {
        previewPath.setAttribute('d', pathData);
        previewPath.setAttribute('stroke', strokeColor);
        previewPath.setAttribute('stroke-width', strokeWidth);
        previewPath.style.display = 'block';
        previewError.style.display = 'none';
        previewSvg.style.opacity = '1';
    } else {
        previewPath.style.display = 'none';
        previewError.style.display = 'flex';
        previewSvg.style.opacity = '0.3';
    }
}

// Set SVG template
function setSvgTemplate(templateName) {
    const path = svgTemplates[templateName];
    if (path) {
        document.getElementById('svg-path-input').value = path;
        updateSvgPreview();
    }
}

// Apply SVG content from modal
function applySvgContent() {
    if (!pendingSvgBox) return;

    const pathData = document.getElementById('svg-path-input').value;
    const strokeColor = document.getElementById('svg-stroke-color').value;
    const strokeWidth = parseInt(document.getElementById('svg-stroke-width').value);

    const { x, y, width, height } = pendingSvgBox;

    createSvgBox(x, y, width, height, pathData, strokeColor, strokeWidth);

    // Close modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('svgContentModal'));
    modal.hide();

    pendingSvgBox = null;
    setTool('select');
}

// Handle modal close without applying
document.addEventListener('DOMContentLoaded', function() {
    const svgModal = document.getElementById('svgContentModal');
    if (svgModal) {
        svgModal.addEventListener('hidden.bs.modal', function() {
            // Just reset the pending box - user cancelled
            pendingSvgBox = null;
        });
    }
});

// Add SVG icon to canvas
function addSvgIcon(iconId, x, y, size = 48) {
    const icon = svgIcons[iconId];
    if (!icon) {
        console.error('SVG icon not found:', iconId);
        return;
    }

    const strokeColor = document.getElementById('strokeColor').value;
    const strokeWidth = parseInt(document.getElementById('strokeWidth').value);

    // Create SVG path
    const pathStr = icon.path;
    const viewBox = icon.viewBox.split(' ').map(Number);
    const viewBoxWidth = viewBox[2];
    const viewBoxHeight = viewBox[3];

    // Calculate scale to fit desired size
    const scale = size / Math.max(viewBoxWidth, viewBoxHeight);

    fabric.loadSVGFromString(
        `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${icon.viewBox}">
            <path d="${pathStr}" fill="none" stroke="${strokeColor}" stroke-width="${strokeWidth / scale}" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>`,
        function(objects, options) {
            const svgGroup = fabric.util.groupSVGElements(objects, options);

            svgGroup.set({
                left: x || (canvas.width / 2),
                top: y || (canvas.height / 2),
                scaleX: scale,
                scaleY: scale,
                originX: 'center',
                originY: 'center',
                objectType: 'svgicon',
                iconId: iconId
            });

            canvas.add(svgGroup);
            canvas.setActiveObject(svgGroup);
            canvas.renderAll();
        }
    );
}

// Add SVG icon from the icon panel (centered on canvas)
function addSvgIconToCanvas(iconId) {
    // Get center of visible canvas area
    const vpt = canvas.viewportTransform;
    const centerX = (-vpt[4] + canvas.width / 2) / zoomLevel;
    const centerY = (-vpt[5] + canvas.height / 2) / zoomLevel;

    addSvgIcon(iconId, centerX, centerY, 64);

    // Switch to select tool after adding
    setTool('select');

    // Note: Panel stays open for easier multi-icon workflow (default is still closed on page load)
}

// Toggle SVG icons panel
let svgIconsPanelOpen = false;

function toggleSvgIconsPanel(forceState) {
    const panel = document.getElementById('svg-icons-panel');
    const chevron = document.getElementById('svg-icons-chevron');
    if (!panel) return;

    if (typeof forceState === 'boolean') {
        svgIconsPanelOpen = forceState;
    } else {
        svgIconsPanelOpen = !svgIconsPanelOpen;
    }

    if (svgIconsPanelOpen) {
        panel.classList.add('show');
        if (chevron) chevron.style.transform = 'rotate(180deg)';
        // Update toggle button
        const toggleBtn = document.getElementById('svg-icons-toggle');
        if (toggleBtn) toggleBtn.classList.add('active');
    } else {
        panel.classList.remove('show');
        if (chevron) chevron.style.transform = 'rotate(0deg)';
        const toggleBtn = document.getElementById('svg-icons-toggle');
        if (toggleBtn) toggleBtn.classList.remove('active');
    }
}

// Toggle Board Templates panel
let boardTemplatesPanelOpen = false;

function toggleBoardTemplatesPanel(forceState) {
    const panel = document.getElementById('board-templates-panel');
    const chevron = document.getElementById('board-templates-chevron');
    if (!panel) return;

    if (typeof forceState === 'boolean') {
        boardTemplatesPanelOpen = forceState;
    } else {
        boardTemplatesPanelOpen = !boardTemplatesPanelOpen;
    }

    if (boardTemplatesPanelOpen) {
        panel.classList.add('show');
        if (chevron) chevron.style.transform = 'rotate(180deg)';
    } else {
        panel.classList.remove('show');
        if (chevron) chevron.style.transform = 'rotate(0deg)';
    }
}

// Handle SVG box drawing
let svgBoxDrawing = false;
let svgBoxStart = null;
let svgBoxPreview = null;

function startSvgBoxDraw(opt) {
    const pointer = canvas.getPointer(opt.e);
    svgBoxDrawing = true;
    svgBoxStart = { x: pointer.x, y: pointer.y };

    const strokeColor = document.getElementById('strokeColor').value;
    const strokeWidth = parseInt(document.getElementById('strokeWidth').value);

    svgBoxPreview = new fabric.Rect({
        left: pointer.x,
        top: pointer.y,
        width: 0,
        height: 0,
        fill: 'transparent',
        stroke: strokeColor,
        strokeWidth: strokeWidth,
        strokeDashArray: [5, 5],
        selectable: false,
        evented: false
    });

    canvas.add(svgBoxPreview);
}

function updateSvgBoxDraw(opt) {
    if (!svgBoxDrawing || !svgBoxPreview) return;

    const pointer = canvas.getPointer(opt.e);
    const width = pointer.x - svgBoxStart.x;
    const height = pointer.y - svgBoxStart.y;

    svgBoxPreview.set({
        width: Math.abs(width),
        height: Math.abs(height),
        left: width > 0 ? svgBoxStart.x : pointer.x,
        top: height > 0 ? svgBoxStart.y : pointer.y
    });

    canvas.renderAll();
}

function endSvgBoxDraw(opt) {
    if (!svgBoxDrawing) return;

    const pointer = canvas.getPointer(opt.e);
    const width = Math.abs(pointer.x - svgBoxStart.x);
    const height = Math.abs(pointer.y - svgBoxStart.y);

    // Remove preview
    if (svgBoxPreview) {
        canvas.remove(svgBoxPreview);
        svgBoxPreview = null;
    }

    // Open SVG content modal if size is meaningful
    if (width > 30 && height > 30) {
        const left = Math.min(svgBoxStart.x, pointer.x);
        const top = Math.min(svgBoxStart.y, pointer.y);
        openSvgContentModal(left, top, width, height);
    }

    svgBoxDrawing = false;
    svgBoxStart = null;
    setTool('select');
}
