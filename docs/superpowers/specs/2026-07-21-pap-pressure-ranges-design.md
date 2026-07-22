# PAP Pressure Range Correction Design

## Objective

Every PAP pressure selector in the Sleep Note Generator must include 30 cm H2O while retaining its existing clinically appropriate minimum.

## Current Problem

The six pressure selectors use three inconsistent data sources:

- Initial and final CPAP read persisted configuration values that currently span 4 through 20.
- Initial and final IPAP use a code-owned range spanning 8 through 30.
- Initial and final EPAP use `Enumerable.Range(4, 23)`, which spans only 4 through 26 because the second argument is a count rather than an ending value.

This means the available maximum depends on the pressure type and, for CPAP, on configuration previously saved in the database.

## Design

The Sleep Note form will own explicit PAP pressure catalogs:

- CPAP: every whole number from 4 through 30, inclusive.
- IPAP: every whole number from 8 through 30, inclusive.
- EPAP: every whole number from 4 through 30, inclusive.

Both the initial and final selector for each pressure type will use the corresponding catalog. The catalogs will be defined with inclusive intent that is clear at the declaration site, avoiding any ambiguity about `Enumerable.Range` counts.

## Persistence Safety

This correction will not reset, reseed, migrate, or overwrite the Sleep Note configuration database. Existing masks, mask sizes, technician names, protocols, and other persisted configuration remain untouched.

The legacy `PressureValues` configuration field will remain in place for compatibility, but the PAP selectors will no longer depend on it. Removing that unused persistence field is outside this focused correction.

## Verification

Automated coverage will verify that:

- Initial and final CPAP contain 30 and retain a minimum of 4.
- Initial and final IPAP contain 30 and retain a minimum of 8.
- Initial and final EPAP contain 30 and retain a minimum of 4.
- A generated multi-stage PAP narrative preserves selected values at the upper limit.
- The complete .NET, frontend, and Playwright suites remain green.

The change will be committed directly to `main`, pushed to `origin/main`, and monitored through the repository check and Koyeb deployment.
