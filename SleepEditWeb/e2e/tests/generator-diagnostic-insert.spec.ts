import { test, expect } from '../fixtures';
import { editor, generateNarrative, openSleepNoteGenerator, selectPageTitle } from '../helpers';

test('diagnostic narrative inserts into the editor even when the page selection is outside it', async ({ page }) => {
  const initialEditorText = await editor(page).innerText();
  await selectPageTitle(page);

  const generator = await openSleepNoteGenerator(page);
  await generator.locator('#pos_Supine').check();
  await generator.locator('#snore_Mild').check();
  await generator.locator('#evt_RespiratoryEvents').check();

  const narrative = await generateNarrative(generator);
  await generator.getByRole('button', { name: 'Insert into Editor' }).click();

  await expect(page.locator('#sleepNoteModal')).not.toHaveClass(/show/);
  await expect(page.locator('.page-title')).toHaveText('Sleep Note Editor');
  await expect(editor(page)).toContainText(narrative);
  await expect(editor(page)).not.toHaveText(initialEditorText);

  await page.reload();
  await expect(editor(page)).toContainText(narrative);
});
