# Task List: Fix Duplicate Medication Submission Race Condition

**Created:** December 19, 2025  
**Problem:** Multiple event handlers can trigger `submitMedication()` causing duplicates when Enter key is pressed  
**Solution:** Simplify event architecture to have a single source of truth (keydown handler only)

---

## Task 1: Analyze current event flow and identify redundant handlers
**Status:** Pending

Review the current MedList/Index.cshtml JavaScript code to document all event handlers that can trigger `submitMedication()`:
1. Autocomplete `select` event
2. Keydown Enter handler on `#filter`
3. Form `submit` event on `#medForm`

**File:** `SleepEditWeb/Views/MedList/Index.cshtml` (lines 73-120)

**Verification:** Document showing all event handlers and the event flow causing duplicates

---

## Task 2: Remove form submit event handler
**Status:** Pending  
**Depends on:** Task 1

Remove the `$('#medForm').on('submit', ...)` handler entirely. It's redundant because:
- We handle Enter key via keydown handler
- No submit button exists anymore
- Form submit event causes duplicate calls

**Implementation:**
```javascript
// DELETE THIS (around line 117-120):
$('#medForm').on('submit', function (e) {
    e.preventDefault();
});
```

**Verification:** No `$('#medForm').on('submit')` code exists

---

## Task 3: Simplify autocomplete select handler
**Status:** Pending  
**Depends on:** Task 2

Modify autocomplete select handler to ONLY set the input value and close the menu. Do NOT call `submitMedication()`.

**Implementation:**
```javascript
// CHANGE FROM (around line 97-103):
select: function(event, ui) {
    event.preventDefault();
    $('#filter').val(ui.item.value);
    $('#filter').autocomplete('close');
    isAutocompleteOpen = false;
    submitMedication();  // REMOVE THIS LINE
    return false;
}

// CHANGE TO:
select: function(event, ui) {
    event.preventDefault();
    $('#filter').val(ui.item.value);
    $('#filter').autocomplete('close');
    isAutocompleteOpen = false;
    // Submission will happen via keydown Enter handler
    return false;
}
```

**Verification:** Autocomplete select handler does not call `submitMedication()`

---

## Task 4: Update keydown handler to be the single submission source
**Status:** Pending  
**Depends on:** Task 3

Simplify keydown Enter handler - remove `isAutocompleteOpen` check since autocomplete select no longer submits.

**Implementation:**
```javascript
// CHANGE FROM (around line 105-113):
$('#filter').on('keydown', function (e) {
    if (e.which === 13) {
        e.preventDefault();
        if (isAutocompleteOpen) {
            return;
        }
        submitMedication();
    }
});

// CHANGE TO:
$('#filter').on('keydown', function (e) {
    if (e.which === 13) {
        e.preventDefault();
        submitMedication();
    }
});
```

**Verification:** Keydown handler is the only code path calling `submitMedication()`

---

## Task 5: Remove unused isAutocompleteOpen and isSubmitting variables
**Status:** Pending  
**Depends on:** Task 4

Clean up JavaScript by removing tracking variables and related event handlers.

**Implementation:**
```javascript
// DELETE variable declarations (around line 78-79):
var isAutocompleteOpen = false;
var isSubmitting = false;

// DELETE autocomplete open/close handlers (around line 91-96):
open: function() {
    isAutocompleteOpen = true;
},
close: function() {
    isAutocompleteOpen = false;
},
```

**Verification:** No `isAutocompleteOpen` or `isSubmitting` variables exist

---

## Task 6: Test the simplified event flow
**Status:** Pending  
**Depends on:** Task 5

Test all submission scenarios:
1. Type medication and press Enter
2. Type partial, select from dropdown with mouse, press Enter
3. Type partial, use arrow keys to highlight, press Enter
4. Type `+newmed` and press Enter
5. Type `-existingmed` and press Enter
6. Type `cls` and press Enter

**Testing steps:**
1. Run locally: `dotnet run` in SleepEditWeb folder
2. Open browser DevTools Network tab
3. Verify only ONE POST to AddMedication per action
4. Check Selected Medications list for duplicates

**Verification:** All scenarios pass with exactly one AJAX submission

---

## Task 7: Commit and push changes
**Status:** Pending  
**Depends on:** Task 6

```bash
git add -A
git commit -m "Fix duplicate submission race condition - simplify to single event handler"
git push
```

Then verify:
1. Koyeb deployment succeeds
2. Production site works: https://sleep-edit.d-german.net

---

## Summary of Changes

**Before:** 3 event handlers could trigger submission
- `autocomplete.select` → calls `submitMedication()`
- `keydown Enter` → calls `submitMedication()`
- `form.submit` → prevented but still causes event timing issues

**After:** 1 event handler triggers submission
- `keydown Enter` → calls `submitMedication()` (ONLY SOURCE)
- `autocomplete.select` → just sets value, closes menu
- `form.submit` → removed entirely

**New Flow:**
1. User types → autocomplete shows
2. User selects from dropdown (click or Enter on highlighted item) → value set, menu closes
3. User presses Enter → keydown handler submits
4. OR: User types full medication → presses Enter → keydown handler submits

This ensures exactly ONE submission per user action.
