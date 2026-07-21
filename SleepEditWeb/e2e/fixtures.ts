import { test as base, expect } from '@playwright/test';

const defaultEditorText = [
  'Sleep Study Note',
  'History:',
  '',
  'Medications:',
  'Medications: none documented.',
  '',
  'Assessment:',
].join('\n');

export const test = base.extend({
  page: async ({ page }, use) => {
    await page.goto('/SleepNoteEditor');
    await expect(page).toHaveTitle(/Sleep Note Editor/);

    const editor = page.locator('#sleepNoteEditor');
    await editor.evaluate((element, value) => {
      element.innerText = value;
      element.dispatchEvent(new InputEvent('input', { bubbles: true }));
    }, defaultEditorText);
    await page.locator('#saveEditorBtn').click();
    await expect(page.locator('#editorStatus')).toHaveText('Saved');

    await use(page);
  },
});

export { expect };
