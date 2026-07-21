import { test, expect } from '../fixtures';

test('editor shell presents tools in workflow order and progressively reveals help', async ({ page }) => {
  const toolLabels = await page.locator('.editor-tool-actions > button').allTextContents();
  expect(toolLabels.map(label => label.trim())).toEqual([
    'Generate Sleep Note',
    'Protocol Viewer',
    'Medication Tool',
  ]);

  const help = page.locator('#workflowHelp');
  await expect(help).not.toHaveClass(/show/);
  await page.getByRole('button', { name: 'How this works' }).click();
  await expect(help).toHaveClass(/show/);
  await expect(help).toContainText('Use Generate Sleep Note for the guided sleep-study form.');

  const initialTheme = await page.locator('html').getAttribute('data-theme');
  await page.locator('#themeToggle').click();
  await expect(page.locator('html')).toHaveAttribute('data-theme', initialTheme === 'dark' ? 'light' : 'dark');
});

test('editor shell does not overflow a mobile viewport', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByRole('heading', { name: 'Sleep Note Editor' })).toBeVisible();

  const dimensions = await page.evaluate(() => ({
    viewportWidth: window.innerWidth,
    documentWidth: document.documentElement.scrollWidth,
  }));
  expect(dimensions.documentWidth).toBeLessThanOrEqual(dimensions.viewportWidth);
});
