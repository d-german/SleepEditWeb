import { test, expect } from '../fixtures';
import { generateNarrative, openSleepNoteGenerator } from '../helpers';

test('multi-stage CPAP to BIPAP ST course keeps exact pressures, rate, and mask changes', async ({ page }) => {
  const generator = await openSleepNoteGenerator(page);
  await generator.locator('#studyTitration').check();

  const therapyStages = generator.locator('.therapy-stage');
  await therapyStages.nth(0).locator('select').nth(1).selectOption('10');
  await generator.locator('#transitionAfter_0').check();
  await generator.locator('#transitionReason_1').selectOption({ label: 'CPAP intolerance' });
  await generator.locator('#initialIpap_1').selectOption('8');
  await generator.locator('#initialEpap_1').selectOption('4');
  await generator.locator('#finalIpap_1').selectOption('30');
  await generator.locator('#finalEpap_1').selectOption('5');

  await expect(generator.locator('#finalIpap_1')).toHaveValue('30');
  await expect(generator.locator('#finalEpap_1')).toHaveValue('5');
  await generator.locator('#transitionAfter_1').check();
  await generator.locator('#transitionReason_2').selectOption({ label: 'Persistent central apneas' });
  await generator.locator('#initialIpap_2').selectOption('10');
  await generator.locator('#initialEpap_2').selectOption('5');
  await generator.locator('#finalIpap_2').selectOption('14');
  await generator.locator('#finalEpap_2').selectOption('6');
  await generator.locator('#backupRate_2').fill('12');

  await generator.locator('#chinStrap_0').check();
  await generator.locator('#maskTransitionAfter_0').check();
  await generator.locator('#maskTransitionReason_1').selectOption({ label: 'Persistent oral leak despite chin strap' });
  const maskTypes = await generator.locator('#maskType_1 option').allTextContents();
  const maskSizes = await generator.locator('#maskSize_1 option').allTextContents();
  await generator.locator('#maskType_1').selectOption({ label: maskTypes.at(-1)! });
  await generator.locator('#maskSize_1').selectOption({ label: maskSizes.at(-1)! });

  const narrative = await generateNarrative(generator);
  expect(narrative).toContain('CPAP');
  expect(narrative).not.toContain('cPAP');
  expect(narrative).toContain('BIPAP');
  expect(narrative).toContain('8/4 cm H2O');
  expect(narrative).toContain('30/5 cm H2O');
  expect(narrative).toContain('BIPAP ST');
  expect(narrative).toContain('12 bpm');
  expect(narrative).toContain('chin strap');
  expect(narrative).toContain('persistent oral leak despite chin strap');
});

test('mask manager adds an option to dropdowns and can remove it again', async ({ page }) => {
  const generator = await openSleepNoteGenerator(page);
  await generator.locator('#studyTitration').check();
  await generator.getByRole('button', { name: 'Manage masks' }).click();

  const dialog = generator.getByRole('dialog', { name: 'Manage masks' });
  await expect(dialog).toBeVisible();
  await dialog.getByRole('textbox', { name: 'New mask type' }).fill('E2E Temporary Mask');
  await dialog.getByRole('button', { name: 'Add' }).first().click();
  await expect(dialog.getByText('E2E Temporary Mask', { exact: true })).toBeVisible();
  await dialog.getByRole('button', { name: 'Done' }).click();
  await expect(generator.locator('#maskType_0')).toContainText('E2E Temporary Mask');

  await generator.getByRole('button', { name: 'Manage masks' }).click();
  page.once('dialog', confirmation => confirmation.accept());
  await generator.getByRole('button', { name: 'Remove E2E Temporary Mask mask type' }).click();
  await expect(dialog.getByText('E2E Temporary Mask', { exact: true })).toHaveCount(0);
  await dialog.getByRole('button', { name: 'Done' }).click();
  await expect(generator.locator('#maskType_0')).not.toContainText('E2E Temporary Mask');
});
