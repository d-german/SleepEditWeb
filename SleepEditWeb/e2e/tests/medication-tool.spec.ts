import { test, expect } from '../fixtures';
import { editor, selectPageTitle } from '../helpers';

async function openMedicationTool(page: import('@playwright/test').Page) {
  await page.getByRole('button', { name: 'Medication Tool' }).click();
  await expect(page.locator('#medToolModal')).toHaveClass(/show/);
}

async function addMedication(page: import('@playwright/test').Page, command: string) {
  await page.locator('#medSearchInput').fill(command);
  await page.locator('#addMedBtn').click();
}

test('insert and replace modes apply medication narratives with a safe cursor fallback', async ({ page }) => {
  await selectPageTitle(page);
  await openMedicationTool(page);
  await addMedication(page, '+melatonin');
  await expect(page.locator('#selectedMedsPreview')).toHaveValue('melatonin');
  await page.locator('#doneMedToolBtn').click();
  await expect(editor(page)).toContainText('Medications: melatonin.');
  await expect(page.locator('.page-title')).toHaveText('Sleep Note Editor');

  await openMedicationTool(page);
  await page.locator('#clearMedsBtn').click();
  await addMedication(page, '+aspirin');
  await page.locator('#insertionMode').selectOption('1');
  await page.locator('#doneMedToolBtn').click();
  await expect(editor(page)).toContainText('Medications: aspirin.');
  await expect(editor(page)).not.toContainText('Medications: melatonin.');
  await expect(editor(page)).not.toContainText('Medications: none documented.');
});

test('medication commands, clipboard modes, and mocked drug information work', async ({ page, context }) => {
  await context.grantPermissions(['clipboard-read', 'clipboard-write']);
  await page.route('**/SleepNoteEditor/DrugInfo?name=*', route => route.fulfill({
    contentType: 'application/json',
    body: JSON.stringify({
      found: true,
      genericName: 'melatonin',
      manufacturer: 'E2E Pharmacy',
      purpose: 'Sleep support',
      uses: 'Mocked end-to-end verification',
      warnings: 'Test warning',
      dosage: 'As directed',
    }),
  }));

  await openMedicationTool(page);
  await addMedication(page, '+melatonin');
  await addMedication(page, '+aspirin');
  await addMedication(page, '-melatonin');
  await expect(page.locator('#selectedMedsPreview')).toHaveValue('aspirin');
  await page.locator('#copyMedsBtn').click();
  await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toBe('aspirin');

  await addMedication(page, 'cls');
  await expect(page.locator('#selectedMedsPreview')).toHaveValue('');
  await addMedication(page, '+zolpidem');
  await page.locator('#insertionMode').selectOption('2');
  await page.locator('#doneMedToolBtn').click();
  await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toContain('zolpidem');

  await openMedicationTool(page);
  await page.locator('#medSearchInput').fill('melatonin');
  await page.locator('#medInfoBtn').click();
  await expect(page.locator('#drugInfoModal')).toHaveClass(/show/);
  await expect(page.locator('#drugInfoContent')).toContainText('Sleep support');
  await expect(page.locator('#drugInfoContent')).toContainText('E2E Pharmacy');
});
