# BiPAP Pressure Warning Boundaries Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Correct BiPAP warning boundaries so a 4 cm H2O difference is accepted while reversed IPAP/EPAP values receive a distinct order warning.

**Architecture:** Keep the validation local to the sleep-note form. Separate pressure-order detection from low-pressure-support detection, render both as non-blocking status warnings, and preserve all selected values.

**Tech Stack:** .NET 8, Blazor/Razor components, Playwright with TypeScript

## Global Constraints

- Initial and final pressure pairs must be evaluated independently.
- A difference of exactly 4 cm H2O must not trigger a low-pressure-support warning.
- EPAP greater than IPAP must trigger an order warning instead of a low-pressure-support warning for that pair.
- The application must never reorder or automatically change selected pressures.
- Do not change models, persistence, masks, protocols, or narrative generation.
- Work on local `main` and do not push until the user reviews the result.

---

### Task 1: Correct the BiPAP pressure warning classification

**Files:**
- Modify: `SleepEditWeb/e2e/tests/generator-pap-mask-course.spec.ts`
- Modify: `SleepEditWeb/Components/SleepNote/SleepNoteForm.razor.cs:229-245`
- Modify: `SleepEditWeb/Components/SleepNote/SleepNoteForm.razor:268-274`

**Interfaces:**
- Consumes: `TherapyStageState.InitialIpap`, `InitialEpap`, `FinalIpap`, and `FinalEpap`
- Produces: `HasInvalidPressureOrder(TherapyStageState)`, `GetInvalidPressureOrderMessage(TherapyStageState)`, and corrected low-pressure-support helpers

- [ ] **Step 1: Write failing Playwright regression tests**

Add these tests to `generator-pap-mask-course.spec.ts`:

```typescript
test('BiPAP warning distinguishes a four-point difference from reversed pressure order', async ({ page }) => {
  const generator = await openSleepNoteGenerator(page);
  await generator.locator('#studyTitration').check();
  await generator.locator('#therapyMode_0_Bipap').check();

  const stage = generator.locator('.therapy-stage').nth(0);
  const warnings = stage.getByRole('status');
  await generator.locator('#finalIpap_0').selectOption('20');
  await generator.locator('#finalEpap_0').selectOption('16');
  await expect(warnings).toHaveCount(0);

  await generator.locator('#finalEpap_0').selectOption('17');
  await expect(warnings).toHaveCount(1);
  await expect(warnings).toContainText('final IPAP/EPAP difference is below 4 cm H2O');

  await generator.locator('#finalIpap_0').selectOption('16');
  await generator.locator('#finalEpap_0').selectOption('20');
  await expect(warnings).toHaveCount(1);
  await expect(warnings).toContainText('final EPAP exceeds IPAP');
  await expect(warnings).not.toContainText('below 4 cm H2O');
  await expect(generator.locator('#finalIpap_0')).toHaveValue('16');
  await expect(generator.locator('#finalEpap_0')).toHaveValue('20');
});

test('initial order and final support warnings are evaluated independently', async ({ page }) => {
  const generator = await openSleepNoteGenerator(page);
  await generator.locator('#studyTitration').check();
  await generator.locator('#therapyMode_0_Bipap').check();

  await generator.locator('#initialIpap_0').selectOption('8');
  await generator.locator('#initialEpap_0').selectOption('10');
  await generator.locator('#finalIpap_0').selectOption('20');
  await generator.locator('#finalEpap_0').selectOption('17');

  const warnings = generator.locator('.therapy-stage').nth(0).getByRole('status');
  await expect(warnings).toHaveCount(2);
  await expect(warnings.nth(0)).toContainText('initial EPAP exceeds IPAP');
  await expect(warnings.nth(1)).toContainText('final IPAP/EPAP difference is below 4 cm H2O');
});
```

- [ ] **Step 2: Run the focused test and verify the expected failure**

Run:

```powershell
Set-Location SleepEditWeb
npx playwright test e2e/tests/generator-pap-mask-course.spec.ts
```

Expected: the new tests fail because reversed pressures display the low-pressure-support warning and no pressure-order warning exists.

- [ ] **Step 3: Implement separate order and support validation**

Replace the existing helpers in `SleepNoteForm.razor.cs` with:

```csharp
private static bool HasInvalidPressureOrder(TherapyStageState stage) =>
    stage.InitialEpap > stage.InitialIpap ||
    stage.FinalEpap > stage.FinalIpap;

private static string GetInvalidPressureOrderMessage(TherapyStageState stage)
{
    var initialIsInvalid = stage.InitialEpap > stage.InitialIpap;
    var finalIsInvalid = stage.FinalEpap > stage.FinalIpap;

    return (initialIsInvalid, finalIsInvalid) switch
    {
        (true, true) => "The initial and final EPAP values exceed IPAP.",
        (true, false) => "The initial EPAP exceeds IPAP.",
        (false, true) => "The final EPAP exceeds IPAP.",
        _ => string.Empty
    };
}

private static bool IsLowPressureSupport(int ipap, int epap) =>
    epap <= ipap && ipap - epap < 4;

private static bool HasLowPressureSupport(TherapyStageState stage) =>
    IsLowPressureSupport(stage.InitialIpap, stage.InitialEpap) ||
    IsLowPressureSupport(stage.FinalIpap, stage.FinalEpap);

private static string GetLowPressureSupportMessage(TherapyStageState stage)
{
    var initialIsLow = IsLowPressureSupport(stage.InitialIpap, stage.InitialEpap);
    var finalIsLow = IsLowPressureSupport(stage.FinalIpap, stage.FinalEpap);

    return (initialIsLow, finalIsLow) switch
    {
        (true, true) => "The initial and final IPAP/EPAP differences are below 4 cm H2O.",
        (true, false) => "The initial IPAP/EPAP difference is below 4 cm H2O.",
        (false, true) => "The final IPAP/EPAP difference is below 4 cm H2O.",
        _ => string.Empty
    };
}
```

Insert this block immediately before the existing low-pressure-support alert in `SleepNoteForm.razor`:

```razor
@if (HasInvalidPressureOrder(stage))
{
    <div class="alert alert-warning px-3 py-2 mb-2 small" role="status">
        Review pressure order: @GetInvalidPressureOrderMessage(stage)
        Values remain exactly as selected.
    </div>
}
```

- [ ] **Step 4: Rebuild and run the focused Playwright test**

Run:

```powershell
dotnet build SleepEditWeb.sln --no-restore
Set-Location SleepEditWeb
npx playwright test e2e/tests/generator-pap-mask-course.spec.ts
```

Expected: all tests in `generator-pap-mask-course.spec.ts` pass.

- [ ] **Step 5: Run complete verification**

Run from the repository root:

```powershell
dotnet test SleepEditWeb.sln --no-restore
Set-Location SleepEditWeb
npm run check:frontend
npx playwright test
Set-Location ..
git diff --check
```

Expected: 0 failed .NET tests, frontend checks pass, all Playwright tests pass, and `git diff --check` returns no output.

- [ ] **Step 6: Commit locally without pushing**

```powershell
git add -- SleepEditWeb/Components/SleepNote/SleepNoteForm.razor.cs SleepEditWeb/Components/SleepNote/SleepNoteForm.razor SleepEditWeb/e2e/tests/generator-pap-mask-course.spec.ts
git commit -m "Fix BiPAP pressure warning boundaries"
```

Expected: the commit is created on local `main`; `origin/main` remains unchanged for user review.
