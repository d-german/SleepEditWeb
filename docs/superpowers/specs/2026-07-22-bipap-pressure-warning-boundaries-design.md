# BiPAP Pressure Warning Boundaries Design

## Problem

The BiPAP pressure-support warning currently subtracts EPAP from IPAP and tests whether the signed result is below 4 cm H2O. When the values are accidentally reversed, such as IPAP 16 and EPAP 20, the result is -4 and the UI incorrectly describes the difference as below 4. The magnitude is exactly 4, while the actual problem is that EPAP exceeds IPAP.

## Considered Approaches

1. **Separate order and pressure-support validation (selected).** Detect EPAP greater than IPAP as its own warning. Only evaluate low pressure support when the pair is in the correct order. This produces accurate, actionable feedback without changing values.
2. **Use the absolute difference only.** This would remove the false low-support warning for 16/20, but it would silently accept a clinically reversed pair.
3. **Automatically reorder or adjust pressures.** This would prevent invalid ordering, but it conflicts with the existing requirement that values remain exactly as selected.

## Selected Behavior

Initial and final IPAP/EPAP pairs are evaluated independently.

- If EPAP exceeds IPAP, show a pressure-order warning for that pair.
- If IPAP is greater than or equal to EPAP and the difference is below 4 cm H2O, show the existing low-pressure-support warning for that pair.
- If the difference is exactly 4 cm H2O, do not show a low-pressure-support warning.
- Preserve every selected value. The UI provides guidance only and never changes a pressure automatically.
- If one pair has an ordering error and the other has low pressure support, show both applicable warnings so one condition cannot hide the other.

Examples:

| IPAP | EPAP | Expected feedback |
| ---: | ---: | --- |
| 20 | 16 | No warning |
| 20 | 17 | Pressure support is below 4 cm H2O |
| 16 | 20 | EPAP exceeds IPAP |
| 16 | 16 | Pressure support is below 4 cm H2O |

## Implementation

Keep the validation in `SleepNoteForm.razor.cs`, alongside the existing therapy-stage helpers. Add order-specific helpers and exclude reversed pairs from the low-pressure-support calculation. Render an order warning in `SleepNoteForm.razor` using the existing non-blocking warning styling and the same statement that values remain exactly as selected.

No model, persistence, mask configuration, protocol, or narrative-generation changes are required.

## Verification

Add a Playwright regression test covering the exact boundary and reversed-order cases:

- 20/16 produces no warning.
- 20/17 produces the low-pressure-support warning.
- 16/20 produces the pressure-order warning and not the low-pressure-support warning.
- Selected pressure values remain unchanged.

Run the focused Playwright test, the complete Playwright suite, the .NET test suite, and frontend checks before committing the implementation. Do not push until the user reviews the local changes.
