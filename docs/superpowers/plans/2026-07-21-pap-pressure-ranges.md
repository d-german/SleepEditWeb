# PAP Pressure Range Correction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every initial and final CPAP, IPAP, and EPAP selector include 30 cm H2O while preserving its existing minimum.

**Architecture:** Keep PAP pressure choices as code-owned form catalogs so an older persisted configuration cannot cap a selector. CPAP and EPAP will span 4–30 inclusive, IPAP will remain 8–30 inclusive, and no database configuration will be migrated or reset.

**Tech Stack:** ASP.NET Core 8, Blazor Server/Razor components, TypeScript, Playwright, NUnit

## Global Constraints

- Initial and final CPAP must contain every whole number from 4 through 30 inclusive.
- Initial and final IPAP must contain every whole number from 8 through 30 inclusive.
- Initial and final EPAP must contain every whole number from 4 through 30 inclusive.
- Do not reset, reseed, migrate, or overwrite masks, mask sizes, technician names, protocols, or other persisted configuration.
- Keep the legacy `SleepNoteConfiguration.PressureValues` persistence field for compatibility; removing it is outside scope.
- Commit directly to `main`, push to `origin/main`, and verify the GitHub check and Koyeb deployment.

---

### Task 1: Correct all PAP pressure catalogs

**Files:**
- Modify: `SleepEditWeb/e2e/tests/generator-pap-mask-course.spec.ts`
- Modify: `SleepEditWeb/Components/SleepNote/SleepNoteForm.razor.cs:39-43`
- Modify: `SleepEditWeb/Components/SleepNote/SleepNoteForm.razor:197-215`

**Interfaces:**
- Consumes: existing `SleepNoteForm` therapy-stage UI and `openSleepNoteGenerator(page)` Playwright helper.
- Produces: `CpapPressureValues`, `BipapIpapValues`, and `BipapEpapValues` catalogs used by every corresponding initial/final selector.

- [ ] **Step 1: Write the failing browser regression test**

In `SleepEditWeb/e2e/tests/generator-pap-mask-course.spec.ts`, change the existing multi-stage test's final CPAP selection from `10` to `30`, assert the generated CPAP narrative reaches 30, and add this focused range test:

```ts
test('every PAP pressure selector reaches 30 while preserving its minimum', async ({ page }) => {
  const generator = await openSleepNoteGenerator(page);
  await generator.locator('#studyTitration').check();

  const firstStage = generator.locator('.therapy-stage').nth(0);
  const cpapSelectors = firstStage.locator('select');
  await expect(cpapSelectors).toHaveCount(2);

  const initialCpap = cpapSelectors.nth(0);
  const finalCpap = cpapSelectors.nth(1);
  expect(await initialCpap.locator('option').allTextContents()).toEqual(
    Array.from({ length: 27 }, (_, index) => String(index + 4))
  );
  expect(await finalCpap.locator('option').allTextContents()).toEqual(
    Array.from({ length: 27 }, (_, index) => String(index + 4))
  );
  await initialCpap.selectOption('30');
  await finalCpap.selectOption('30');

  await generator.locator('#therapyMode_0_Bipap').check();

  const expectedIpapValues = Array.from({ length: 23 }, (_, index) => String(index + 8));
  const expectedEpapValues = Array.from({ length: 27 }, (_, index) => String(index + 4));
  for (const selector of ['#initialIpap_0', '#finalIpap_0']) {
    const pressure = generator.locator(selector);
    expect(await pressure.locator('option').allTextContents()).toEqual(expectedIpapValues);
    await pressure.selectOption('30');
    await expect(pressure).toHaveValue('30');
  }
  for (const selector of ['#initialEpap_0', '#finalEpap_0']) {
    const pressure = generator.locator(selector);
    expect(await pressure.locator('option').allTextContents()).toEqual(expectedEpapValues);
    await pressure.selectOption('30');
    await expect(pressure).toHaveValue('30');
  }
});
```

In the existing multi-stage test, add:

```ts
expect(narrative).toContain('titrated to 30 cm H2O');
```

- [ ] **Step 2: Run the focused test and verify the expected failure**

Run from `SleepEditWeb/`:

```powershell
npx playwright test e2e/tests/generator-pap-mask-course.spec.ts
```

Expected: FAIL because CPAP does not contain `30`; the focused range assertion also reports CPAP ending at `20` and EPAP ending at `26`.

- [ ] **Step 3: Implement explicit inclusive pressure catalogs**

In `SleepEditWeb/Components/SleepNote/SleepNoteForm.razor.cs`, replace the current PAP range declarations with:

```csharp
private static readonly IReadOnlyList<int> CpapPressureValues =
    Enumerable.Range(4, 27).ToArray();

private static readonly IReadOnlyList<int> BipapIpapValues =
    Enumerable.Range(8, 23).ToArray();

private static readonly IReadOnlyList<int> BipapEpapValues =
    Enumerable.Range(4, 27).ToArray();
```

In both CPAP selector loops in `SleepEditWeb/Components/SleepNote/SleepNoteForm.razor`, replace:

```razor
@foreach (var p in _config.PressureValues)
```

with:

```razor
@foreach (var p in CpapPressureValues)
```

Do not edit `LiteDbSleepNoteConfigRepository`, reset configuration, or alter any persisted mask or protocol data.

- [ ] **Step 4: Run the focused browser test and verify it passes**

Run from `SleepEditWeb/`:

```powershell
npx playwright test e2e/tests/generator-pap-mask-course.spec.ts
```

Expected: all tests in the file PASS; all six pressure selectors accept 30 and retain their documented minima.

- [ ] **Step 5: Run the complete verification suite**

Run from the repository root:

```powershell
dotnet test SleepEditWeb.sln --no-restore
```

Run from `SleepEditWeb/`:

```powershell
npm run check:frontend
npx playwright test
```

Expected: .NET tests, frontend lint/tests, and the complete Playwright suite all PASS.

- [ ] **Step 6: Commit the correction**

```powershell
git add SleepEditWeb/Components/SleepNote/SleepNoteForm.razor.cs SleepEditWeb/Components/SleepNote/SleepNoteForm.razor SleepEditWeb/e2e/tests/generator-pap-mask-course.spec.ts docs/superpowers/plans/2026-07-21-pap-pressure-ranges.md
git commit -m "Fix all PAP pressure ranges through 30"
```

Expected: one focused implementation commit on `main` with a clean working tree.

### Task 2: Push and verify deployment

**Files:**
- No file changes.

**Interfaces:**
- Consumes: the verified `main` commit from Task 1.
- Produces: matching local and remote `main` revisions plus live Koyeb evidence.

- [ ] **Step 1: Push `main`**

```powershell
git push origin main
```

Expected: `origin/main` advances to the implementation commit without force-push.

- [ ] **Step 2: Verify the GitHub workflow**

Use the GitHub operations workflow to inspect the newest `main` run.

Expected: `Frontend Guardrails` completes with conclusion `success` for the implementation commit.

- [ ] **Step 3: Verify Koyeb and the live UI**

Inspect the authenticated Koyeb dashboard and `https://sleep-edit.d-german.net/SleepNoteEditor`.

Expected: the newest SleepEditWeb deployment reports `Healthy` with `1 of 1 running`, and live CPAP, IPAP, and EPAP selectors contain and accept `30`.

- [ ] **Step 4: Confirm final repository state**

```powershell
git status --porcelain=v1
git rev-parse HEAD
git ls-remote origin refs/heads/main
```

Expected: the working tree is clean and local `HEAD` matches `origin/main`.
