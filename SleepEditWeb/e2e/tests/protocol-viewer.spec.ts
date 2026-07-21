import { test, expect } from '../fixtures';
import { editor, selectPageTitle } from '../helpers';

test('protocol viewer inserts selected content into the editor and preserves the page shell', async ({ page }) => {
  await selectPageTitle(page);
  await page.getByRole('button', { name: 'Protocol Viewer' }).click();
  const viewer = page.frameLocator('#protocolViewerFrame');
  await expect(viewer.locator('#protocolViewerOkBtn')).toBeVisible();

  const diagnosticTab = viewer.getByRole('tab', { name: /Diagnostic Polysomnogram/ });
  await diagnosticTab.click();
  const protocolItem = 'Monitor SpO2 and EKG for Emergency Guideline Interventions';
  await viewer.getByRole('checkbox', { name: protocolItem }).check();
  await viewer.locator('#protocolViewerOkBtn').click();

  await expect(page.locator('#protocolViewerModal')).not.toHaveClass(/show/);
  await expect(page.locator('.page-title')).toHaveText('Sleep Note Editor');
  await expect(editor(page)).toContainText(protocolItem);
});

test('protocol viewer cancel closes without changing the editor', async ({ page }) => {
  const original = await editor(page).innerText();
  await page.getByRole('button', { name: 'Protocol Viewer' }).click();
  const viewer = page.frameLocator('#protocolViewerFrame');
  await expect(viewer.locator('#protocolViewerCancelBtn')).toBeVisible();
  await viewer.locator('#protocolViewerCancelBtn').click();

  await expect(page.locator('#protocolViewerModal')).not.toHaveClass(/show/);
  expect(await editor(page).innerText()).toBe(original);
});
