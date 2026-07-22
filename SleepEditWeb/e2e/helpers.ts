import type { FrameLocator, Page } from '@playwright/test';
import { expect } from '@playwright/test';

export const editor = (page: Page) => page.locator('#sleepNoteEditor');

export async function saveEditor(page: Page): Promise<void> {
  const ok = await page.evaluate(async () => {
    const element = document.getElementById('sleepNoteEditor');
    const token = document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value ?? '';
    const response = await fetch('/SleepNoteEditor/Save', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
      body: JSON.stringify({ content: element?.innerText ?? '' }),
    });
    return response.ok;
  });
  expect(ok).toBe(true);
}

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
