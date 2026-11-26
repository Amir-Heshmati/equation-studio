import assert from 'node:assert/strict';
import { chromium, devices } from 'playwright';

const [url, screenshotPath] = process.argv.slice(2);
if (!url || !screenshotPath) {
    throw new Error('Usage: node smoke.mjs <site-url> <screenshot-path>');
}

const browser = await chromium.launch();

try {
    await checkDesktop();
    await checkTouch();
} finally {
    await browser.close();
}

async function checkDesktop() {
    const page = await browser.newPage({ viewport: { width: 1400, height: 920 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));

    try {
        await openApp(page);
        await enter(page, 'x / 3 = 1 / 2', 'Solve');
        await page.locator('.answer').getByText('x = 3/2', { exact: true }).waitFor();
        assert.match(await page.locator('.graph figcaption').innerText(), /Intersection: \(3\/2, 1\/2\)/);
        await page.screenshot({ path: screenshotPath, fullPage: true, animations: 'disabled' });

        await enter(page, 'x + 1 = x + 1.000000000000000001', 'Solve');
        await page.locator('.answer').getByText('No solution', { exact: true }).waitFor();
        const scaleNote = page.locator('.graph-scale-note');
        await scaleNote.waitFor();
        assert.match(await scaleNote.innerText(), /too close to distinguish at this scale/);
        assert.match(await scaleNote.innerText(), /1\/1000000000000000000/);

        await enter(page, 'x = x + 4', 'Solve');
        await page.locator('.answer').getByText('No solution', { exact: true }).waitFor();
        await scaleNote.waitFor({ state: 'hidden' });

        await enter(page, 'x / 0 = 1', 'Solve');
        const inputError = page.locator('.error[role="alert"]');
        await inputError.waitFor();
        assert.equal(await inputError.innerText(), 'Division by zero is undefined.');

        await enter(page, 'x = 3', 'Plot as a line');
        await page.locator('.answer').getByText('x = 3', { exact: true }).waitFor();
        const vertical = page.locator('.graph .left-line');
        assert.equal(await vertical.getAttribute('x1'), await vertical.getAttribute('x2'));

        await page.getByLabel('Equation or expression').fill('');
        await page.getByLabel('Equation or expression').pressSequentially('2z');
        assert.equal(await page.getByLabel('Equation or expression').inputValue(), '2');
        assert.deepEqual(errors, []);
        console.log('Desktop passed: exact fractions, near-parallel warning and gap, division by zero, vertical plot, and input filter.');
    } finally {
        await page.close();
    }
}

async function checkTouch() {
    const context = await browser.newContext({ ...devices['Pixel 7'] });
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));

    try {
        await openApp(page);
        const input = page.getByLabel('Equation or expression');
        await input.fill('');
        await input.tap();
        await page.locator('details[data-math-keypad][open]').waitFor();
        for (const label of ['2', 'Variable x', 'Plus', '5']) {
            await page.getByRole('button', { name: label, exact: true }).tap();
        }
        assert.equal(await input.inputValue(), '2x+5');
        await page.getByRole('button', { name: 'Plot', exact: true }).tap();
        await page.locator('.answer').getByText('y = 2x + 5', { exact: true }).waitFor();
        await page.locator('.graph .left-line').waitFor();

        const reset = page.getByRole('button', { name: 'Reset view', exact: true });
        const plottedLine = page.locator('.graph .left-line');
        const originalY = await plottedLine.getAttribute('y1');
        const originalX = await plottedLine.getAttribute('x1');
        await page.getByRole('button', { name: 'Zoom in horizontally', exact: true }).tap();
        await page.waitForFunction(expected => document.querySelector('.graph .left-line')?.getAttribute('x1') !== expected, originalX);
        assert.equal(await plottedLine.getAttribute('y1'), originalY);
        await reset.tap();
        await page.waitForFunction(expected => document.querySelector('.graph .left-line')?.getAttribute('x1') === expected, originalX);

        const graph = page.locator('.interactive-graph');
        await graph.scrollIntoViewIfNeeded();
        await page.waitForFunction(() => performance.getEntriesByType('resource')
            .some(entry => entry.name.includes('/js/graph-gestures.js')));

        const axisSelector = '.axis-grab[data-graph-gesture="y"]';
        const axis = page.locator(axisSelector);
        const original = await axis.getAttribute('x1');
        const box = await graph.boundingBox();
        assert.ok(box);
        const start = { x: Math.round(box.x + box.width * 0.25), y: Math.round(box.y + box.height * 0.36) };
        const session = await context.newCDPSession(page);
        await session.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [start] });
        for (const offset of [10, 20, 36]) {
            await session.send('Input.dispatchTouchEvent', {
                type: 'touchMove',
                touchPoints: [{ x: start.x + offset, y: start.y + 8 }]
            });
        }
        await session.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
        await session.detach();
        await page.waitForFunction(({ selector, original }) =>
            document.querySelector(selector)?.getAttribute('x1') !== original, { selector: axisSelector, original });
        assert.ok(await axis.getAttribute('x1'));
        await page.locator('.interactive-graph[data-dragging]').waitFor({ state: 'hidden' });
        // Verify reset after the CDP gesture with keyboard activation, independently of touch tap synthesis.
        await reset.press('Enter');
        await page.waitForFunction(({ selector, original }) =>
            document.querySelector(selector)?.getAttribute('x1') === original, { selector: axisSelector, original });
        assert.deepEqual(errors, []);
        console.log('Touch emulation passed: keypad, plot, touch drag, horizontal zoom, and reset.');
    } finally {
        await context.close();
    }
}

async function openApp(page) {
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.getByRole('heading', { name: 'Linear equation solver' }).waitFor({ timeout: 30_000 });
}

async function enter(page, input, action) {
    await page.getByLabel('Equation or expression').fill(input);
    await page.getByRole('button', { name: action, exact: true }).click();
}
