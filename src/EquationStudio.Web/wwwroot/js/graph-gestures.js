export function connect(svg, dotnet) {
    const controller = new AbortController();
    const options = { signal: controller.signal };
    const changes = [];
    let activePointer = null;
    let frame = 0;
    let sending = null;

    function coordinates(event) {
        const matrix = svg.getScreenCTM();
        return matrix && new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    }

    function requestUpdate() {
        if (!controller.signal.aborted && !frame && !sending && changes.length > 0) {
            frame = requestAnimationFrame(sendChanges);
        }
    }

    function queue(mode, dx, dy) {
        if (mode === 'x') dy = 0;
        if (mode === 'y') dx = 0;
        if (dx === 0 && dy === 0) return;
        const last = changes.at(-1);
        if (last?.mode === mode) {
            last.dx += dx;
            last.dy += dy;
        } else {
            changes.push({ mode, dx, dy });
        }
        requestUpdate();
    }

    async function sendChanges() {
        frame = 0;
        const { mode, dx, dy } = changes.shift();
        sending = dotnet.invokeMethodAsync('MoveGraph', mode, dx, dy);
        try {
            await sending;
        } catch (error) {
            if (!controller.signal.aborted) console.error('Graph input failed.', error);
        } finally {
            sending = null;
            requestUpdate();
        }
    }

    function start(event) {
        if (activePointer || event.button !== 0) return;
        const point = coordinates(event);
        if (!point) return;
        const mode = event.target.closest('[data-graph-gesture]')?.dataset.graphGesture ?? 'pan';

        event.preventDefault();
        svg.focus({ preventScroll: true });
        svg.setPointerCapture(event.pointerId);
        svg.dataset.dragging = mode;
        activePointer = { id: event.pointerId, mode, point };
    }

    function move(event) {
        if (activePointer?.id !== event.pointerId) return;
        const point = coordinates(event);
        if (!point) return;
        const { mode, point: previous } = activePointer;
        activePointer.point = point;
        queue(mode, point.x - previous.x, point.y - previous.y);
    }

    function end(event) {
        if (activePointer?.id !== event.pointerId) return;
        if (event.type === 'pointerup') move(event);
        activePointer = null;
        delete svg.dataset.dragging;
    }

    function keydown(event) {
        if (event.ctrlKey || event.altKey || event.metaKey) return;
        const distances = { ArrowLeft: [36, 0], ArrowRight: [-36, 0], ArrowUp: [0, 36], ArrowDown: [0, -36] };
        if (!Object.hasOwn(distances, event.key)) return;
        const [dx, dy] = distances[event.key];
        event.preventDefault();
        if (event.shiftKey) {
            queue(dx ? 'x' : 'y', -dx, -dy);
        } else {
            queue('pan', dx, dy);
        }
    }

    svg.addEventListener('pointerdown', start, options);
    svg.addEventListener('pointermove', move, options);
    svg.addEventListener('pointerup', end, options);
    svg.addEventListener('pointercancel', end, options);
    svg.addEventListener('lostpointercapture', end, options);
    svg.addEventListener('keydown', keydown, options);

    function stopPendingMotion() {
        if (frame) cancelAnimationFrame(frame);
        frame = 0;
        changes.length = 0;
        const activeId = activePointer?.id;
        activePointer = null;
        delete svg.dataset.dragging;
        if (activeId !== undefined && svg.hasPointerCapture(activeId)) {
            svg.releasePointerCapture(activeId);
        }
    }

    return {
        async cancelMotion() {
            // A final animation frame must not pan a view which has just been reset.
            stopPendingMotion();
            if (sending) await sending.catch(() => {});
        },
        disconnect() {
            controller.abort();
            stopPendingMotion();
        }
    };
}
