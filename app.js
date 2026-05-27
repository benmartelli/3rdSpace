let canvas;
let isDrawing = false;
const wrapper = document.getElementById('site-wrapper');

// Initialize FabricJS Canvas on window load
window.addEventListener('load', () => {
    canvas = new fabric.Canvas('miro-canvas', {
        width: window.innerWidth,
        height: window.innerHeight,
        selection: true
    });

    // Handle resizing window smoothly
    window.addEventListener('resize', () => {
        canvas.setWidth(window.innerWidth);
        canvas.setHeight(window.innerHeight);
        canvas.renderAll();
    });
});

// Toggle between viewing your site and using the Miro overlay
function setMode(mode) {
    const btnWeb = document.getElementById('btn-web');
    const btnMiro = document.getElementById('btn-miro');

    if (mode === 'web') {
        wrapper.classList.remove('miro-mode-active');
        btnWeb.classList.add('active');
        btnMiro.classList.remove('active');
        
        // Turn off drawing if it was on
        canvas.isDrawingMode = false;
        document.getElementById('whiteboard-tools').children[1].innerText = "🖌️ Draw (Off)";
        isDrawing = false;
    } else {
        wrapper.classList.add('miro-mode-active');
        btnMiro.classList.add('active');
        btnWeb.classList.remove('active');
    }
}

// MIRO TOOL: Add a Sticky Note anywhere
function addStickyNote() {
    // Generate a group containing a yellow square and an editable text box
    const rect = new fabric.Rect({
        width: 150,
        height: 150,
        fill: '#fff7a1',
        shadow: 'rgba(0,0,0,0.2) 3px 3px 7px'
    });

    const text = new fabric.IText('Type anywhere\n(Double click)', {
        fontSize: 16,
        left: 15,
        top: 15,
        width: 120,
        fontFamily: 'sans-serif'
    });

    const stickyNote = new fabric.Group([rect, text], {
        left: window.innerWidth / 2 - 75,
        top: window.innerHeight / 2 - 75,
    });

    canvas.add(stickyNote);
    canvas.setActiveObject(stickyNote);
    canvas.renderAll();
}

// MIRO TOOL: Toggle Freestyle Brush Drawing
function toggleDrawing() {
    isDrawing = !isDrawing;
    canvas.isDrawingMode = isDrawing;
    
    const drawBtn = document.getElementById('whiteboard-tools').children[1];
    if (isDrawing) {
        drawBtn.innerText = "🖌️ Drawing (ON)";
        canvas.freeDrawingBrush.color = "#ff007f"; // Hot pink drawing line
        canvas.freeDrawingBrush.width = 4;
    } else {
        drawBtn.innerText = "🖌️ Draw (Off)";
    }
}

// MIRO TOOL: Clear Canvas
function clearCanvas() {
    if(confirm("Are you sure you want to clear your board?")) {
        canvas.clear();
    }
}
