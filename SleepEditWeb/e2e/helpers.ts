import type { FrameLocator, Page } from '@playwright/test';
import { expect } from './fixtures';

export const editor = (page: Page) => page.locator('#sleepNoteEditor');

export async function selectPageTitle(page: Page): Promise<void> {
  await page.evaluate(() => {
    const title = document.querySelector('.page-title');
    if (!title) {
      throw new Error('Page title was not found');
    }

    const range = document.createRange();
    range.selectNodeContents(title);
    const selection = window.getSelection();
    selection?.removeAllRanges();
    selection?.addRange(range);
  });
}

export async function openSleepNoteGenerator(page: Page): Promise<FrameLocator> {
  await page.getByRole('button', { name: 'Generate Sleep Note' }).click();
  const frame = page.frameLocator('#sleepNoteFrame');
  await expect(frame.getByRole('group', { name: 'Study Performed' })).toBeVisible();
  return frame;
}

export async function generateNarrative(frame: FrameLocator): Promise<string> {
  await frame.getByRole('button', { name: 'Generate Note', exact: true }).click();
  const narrative = frame.locator('textarea');
  await expect(narrative).not.toHaveValue('');
  return narrative.inputValue();
}
