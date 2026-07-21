import { test, expect } from '../fixtures';
import { editor, generateNarrative, openSleepNoteGenerator } from '../helpers';

test('reset clears generator choices and cancel leaves the editor unchanged', async ({ page }) => {
  const original = await editor(page).innerText();
  const generator = await openSleepNoteGenerator(page);
  await generator.locator('#pos_Lateral').check();
  await generateNarrative(generator);
  await expect(generator.locator('textarea')).toBeVisible();

  await generator.getByRole('button', { name: 'Reset' }).click();
  await expect(generator.locator('#pos_Lateral')).not.toBeChecked();
  await expect(generator.locator('textarea')).toHaveCount(0);

  await generator.getByRole('button', { name: 'Cancel' }).click();
  await expect(page.locator('#sleepNoteModal')).not.toHaveClass(/show/);
  expect(await editor(page).innerText()).toBe(original);
});
