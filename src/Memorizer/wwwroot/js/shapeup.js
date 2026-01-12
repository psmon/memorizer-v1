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
        } else if (isDrawing) {
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
        if (isDrawing) {
            handleDrawEnd(opt);
        }
    });

    // Keyboard shortcuts
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Delete' || e.key === 'Backspace') {
            if (!e.target.matches('input, textarea')) {
                deleteSelected();
            }
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
