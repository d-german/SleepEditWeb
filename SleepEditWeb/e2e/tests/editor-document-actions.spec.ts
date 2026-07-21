import { test, expect } from '../fixtures';
import { editor } from '../helpers';

test('direct editing, formatting, save, reload, and print preserve the document', async ({ page }) => {
  await editor(page).evaluate(element => {
    element.innerText = 'Formatted sleep note';
    const range = document.createRange();
    range.selectNodeContents(element);
    const selection = window.getSelection();
    selection?.removeAllRanges();
    selection?.addRange(range);
  });

  await page.getByTitle('Bold').click();
  await expect(editor(page).locator('b, strong')).toContainText('Formatted sleep note');

  await page.locator('#saveEditorBtn').click();
  await expect(page.locator('#editorStatus')).toHaveText('Saved');
  await page.reload();
  await expect(editor(page)).toHaveText('Formatted sleep note');

  await page.evaluate(() => {
    const capture = { html: '', focused: false, printed: false, closed: false };
    Object.defineProperty(window, '__printCapture', { value: capture, configurable: true });
    window.open = () => ({
      document: {
        write: (html: string) => { capture.html = html; },
        close: () => undefined,
      },
      focus: () => { capture.focused = true; },
      print: () => { capture.printed = true; },
      close: () => { capture.closed = true; },
    }) as unknown as Window;
  });
  await page.locator('#printEditorBtn').click();
  await expect(page.locator('#editorStatus')).toHaveText('Print dialog opened');
  const printCapture = await page.evaluate(() => (window as typeof window & {
    __printCapture: { html: string; focused: boolean; printed: boolean; closed: boolean };
  }).__printCapture);
  expect(printCapture.html).toContain('Formatted sleep note');
  expect(printCapture).toMatchObject({ focused: true, printed: true, closed: true });
});

test('formatting commands never modify a selection outside the editor', async ({ page }) => {
  await page.evaluate(() => {
    const heading = document.querySelector('.page-title');
    if (!heading) throw new Error('Heading missing');
    const range = document.createRange();
    range.selectNodeContents(heading);
    const selection = window.getSelection();
    selection?.removeAllRanges();
    selection?.addRange(range);
  });

  await page.getByTitle('Italic').click();
  await expect(page.locator('.page-title')).toHaveText('Sleep Note Editor');
  await expect(page.locator('.page-title i, .page-title em')).toHaveCount(0);
});
