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
    const leftPos = window.innerWidth / 2 - 75;
    const topPos = window.innerHeight / 2 - 75;

    // 1. Create the yellow background post-it
    const rect = new fabric.Rect({
        width: 150,
        height: 150,
        fill: '#fff7a1',
        shadow: 'rgba(0,0,0,0.15) 3px 3px 7px',
        left: leftPos,
        top: topPos,
        hasControls: false // Keeps it a clean square shape
    });

    // 2. Create the editable text box layer
    const text = new fabric.Textbox('Type here...', {
        width: 130,
        fontSize: 16,
        left: leftPos + 10, // Slight padding from left edge
        top: topPos + 15,   // Slight padding from top edge
        fontFamily: 'sans-serif',
        hasControls: false,
        splitByGrapheme: true // Prevents words from breaking awkwardly
    });

    // 3. Add them to the canvas as separate elements
    canvas.add(rect);
    canvas.add(text);

    // 4. Group movement trick: When the text or rect moves, move the other!
    text.on('moving', function () {
        rect.set({ left: text.left - 10, top: text.top - 15 });
        canvas.renderAll();
    });

    rect.on('moving', function () {
        text.set({ left: rect.left + 10, top: rect.top + 15 });
        canvas.renderAll();
    });

    // Automatically focus on the text box so the user can type immediately
    canvas.setActiveObject(text);
    text.enterEditing();
    text.selectAll();
    canvas.renderAll();
    }
