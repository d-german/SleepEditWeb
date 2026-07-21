import { test, expect } from '@playwright/test';

test('Admin requires password, persists the session, and locks again on logout', async ({ page }) => {
  await page.goto('/Admin/Medications');
  await expect(page).toHaveURL(/\/Admin\/Login\?returnUrl=/);
  await expect(page.getByRole('heading', { name: 'Admin Access' })).toBeVisible();

  await page.getByLabel('Admin password').fill('wrong');
  await page.getByRole('button', { name: 'Unlock Admin' }).click();
  await expect(page.getByRole('alert')).toHaveText('Incorrect password.');
  await expect(page.getByLabel('Admin password')).toHaveValue('');

  await page.getByLabel('Admin password').fill('sleep123');
  await page.getByRole('button', { name: 'Unlock Admin' }).click();
  await expect(page).toHaveURL(/\/Admin\/Medications/);
  await expect(page.getByRole('heading', { name: 'Admin Dashboard' })).toBeVisible();

  await page.goto('/ProtocolEditor');
  await expect(page).not.toHaveURL(/\/Admin\/Login/);

  const exportResponse = await page.request.get('/Admin/Medications/Export');
  expect(exportResponse.status()).toBe(200);
  expect(exportResponse.headers()['content-type']).toContain('application/json');

  await page.goto('/Admin/Medications');
  await page.getByRole('button', { name: 'Log out' }).click();
  await expect(page).toHaveURL(/\/Admin\/Login/);
  await page.goto('/Admin/Medications');
  await expect(page).toHaveURL(/\/Admin\/Login\?returnUrl=/);
  await page.goto('/ProtocolEditor');
  await expect(page).toHaveURL(/\/Admin\/Login\?returnUrl=/);
});

test('Admin password never appears in navigation or page markup', async ({ page }) => {
  await page.goto('/SleepNoteEditor');
  const adminLink = page.getByRole('link', { name: 'Admin' });
  await expect(adminLink).toHaveAttribute('href', '/Admin/Medications');
  await expect(page.locator('html')).not.toContainText('sleep123');
});
