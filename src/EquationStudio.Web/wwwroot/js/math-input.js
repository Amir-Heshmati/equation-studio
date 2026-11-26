const inputSelector = '[data-math-input]';
const mathCharacters = /^[xXyYｘＸｙＹ0-9۰-۹٠-٩０-９+\-−－＋*×·⋅＊/÷⁄／.٫．=＝()[\]{} ]*$/u;

export function mountMathInput(root, prefersTouchKeypad = () => matchMedia('(any-pointer: coarse)').matches) {
    const snapshots = new WeakMap();
    const accept = value => mathCharacters.test(value);
    const isField = target => target.matches?.(inputSelector);

    function feedback(field, message = '') {
        const status = field.closest('[data-math-entry]')?.querySelector('[data-math-feedback]');
        if (status && status.textContent !== message) status.textContent = message;
    }

    function remember(field) {
        if (accept(field.value)) {
            snapshots.set(field, {
                value: field.value,
                start: field.selectionStart,
                end: field.selectionEnd
            });
        }
    }

    function notifyInput(field) {
        // Publish virtual-key and clipboard edits through the same event that Blazor binds for typing.
        field.dispatchEvent(new Event('input', { bubbles: true }));
    }

    function replace(field, text, start = field.selectionStart, end = field.selectionEnd) {
        if (field.maxLength >= 0 && field.value.length - (end - start) + text.length > field.maxLength) {
            feedback(field, 'This expression is too long.');
            return;
        }

        field.setRangeText(text, start, end, 'end');
        feedback(field);
        notifyInput(field);
    }

    root.addEventListener('focusin', event => {
        const field = event.target;
        if (!isField(field)) return;
        remember(field);
        if (prefersTouchKeypad()) {
            const keypad = field.closest('[data-math-entry]')?.querySelector('[data-math-keypad]');
            if (keypad) keypad.open = true;
        }
    });

    root.addEventListener('beforeinput', event => {
        const field = event.target;
        if (!isField(field) || event.isComposing) return;
        remember(field);
        if (event.inputType?.startsWith('insert') && event.data && !accept(event.data)) {
            event.preventDefault();
            feedback(field, 'Use x, y, numbers and math symbols.');
        }
    }, true);

    root.addEventListener('input', event => {
        const field = event.target;
        if (!isField(field)) return;
        if (event.isComposing) {
            event.stopPropagation();
            return;
        }

        if (accept(field.value)) {
            remember(field);
            feedback(field);
            return;
        }

        // Some keyboards and auto-fill paths omit a cancelable beforeinput event.
        const previous = snapshots.get(field) ?? { value: '', start: 0, end: 0 };
        field.value = previous.value;
        field.setSelectionRange(previous.start, previous.end);
        feedback(field, 'Use x, y, numbers and math symbols.');
    }, true);

    root.addEventListener('compositionstart', event => {
        if (isField(event.target)) remember(event.target);
    }, true);

    root.addEventListener('compositionend', event => {
        if (isField(event.target)) notifyInput(event.target);
    }, true);

    root.addEventListener('paste', event => {
        const field = event.target;
        if (!isField(field) || !event.clipboardData) return;
        event.preventDefault();
        const text = event.clipboardData.getData('text/plain')
            .replace(/\s+/gu, ' ')
            .trim()
            .replace(/^f *\( *[xX] *\) *[=＝] */iu, 'y = ');

        // Reject the whole insertion so unsupported input isn't silently changed into another equation.
        if (!accept(text)) {
            feedback(field, 'Paste an expression with x, y, numbers and math symbols.');
            return;
        }

        replace(field, text);
    }, true);

    const keySelector = '[data-math-key], [data-math-edit]';
    root.addEventListener('pointerdown', event => {
        if (event.target.closest?.(keySelector)) event.preventDefault();
    });

    root.addEventListener('click', event => {
        const key = event.target.closest?.(keySelector);
        if (!key) return;
        const field = key.closest('[data-math-entry]')?.querySelector(inputSelector);
        if (!field) return;

        if (event.detail > 0) field.focus({ preventScroll: true });
        if (key.dataset.mathEdit === 'clear') {
            replace(field, '', 0, field.value.length);
        } else if (key.dataset.mathEdit === 'backspace') {
            const end = field.selectionEnd;
            const start = field.selectionStart === end ? Math.max(0, end - 1) : field.selectionStart;
            replace(field, '', start, end);
        } else {
            replace(field, key.dataset.mathKey);
        }
    });
}

if (typeof document !== 'undefined') mountMathInput(document);
