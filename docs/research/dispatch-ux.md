# UX guidance for the NEMT dispatch and driver MVP

## Decision summary

The MVP should use two deliberately different surfaces:

- A desktop **day board** for the dispatcher: compact, sortable rows; persistent filters; batch actions; and a details panel for the selected trip or journey.
- A mobile **next-action list** for drivers: one trip at a time, large labeled actions, explicit sync state, and almost no data entry.

For a large manifest, do **not** make the dispatcher approve every calculated pickup time. Calculate suggestions for all eligible trips, apply them as a reversible batch, and move only exceptions to a review queue. The dispatcher should see a summary such as `48 ready · 3 need review · 1 blocked`, inspect the three exceptions, and undo or edit any suggestion. This is a product recommendation inferred from the user's stated workflow plus standards that discourage redundant entry and require important data changes to be reviewable or reversible—not a claim that a standard prescribes this exact interaction. [WCAG 2.2 explains why previously supplied information should be auto-populated or selectable](https://www.w3.org/WAI/WCAG22/Understanding/redundant-entry), while its error-prevention guidance supports reversible changes or review-and-correct flows for important stored data. [W3C: Error Prevention](https://www.w3.org/WAI/WCAG22/Understanding/error-prevention-legal-financial-data.html)

## 1. Manifest import: one guided flow, exception-first review

Use a short linear flow:

1. **Upload manifest** — one prominent file control, filename, manifest date, and replacement affordance.
2. **Check trips** — summary counts for ready, warning, and blocked; open directly on `Needs review` when problems exist.
3. **Add schedule** — compute pickup suggestions in bulk; show the formula and defaults once, not in every row.
4. **Finish import** — state exactly how many trips will be added, updated, unchanged, or held back.

Use a step indicator only for this genuinely linear, multi-screen task. The USWDS recommends step indicators for three or more high-level steps in a linear process, and not as navigation for nonlinear work. [USWDS: Step indicator](https://designsystem.digital.gov/components/step-indicator/)

Do not ask the user to repair the CSV in Excel. Errors should be attached to the affected row and described in text with a proposed correction. W3C requires detected errors to identify the item and describe the problem in text; color alone is insufficient. [W3C: Error Identification](https://www.w3.org/WAI/WCAG22/Understanding/error-identification)

Recommended import interaction:

- Default tab: **Needs review (3)**; other tabs: **Ready (48)** and **All (51)**.
- Each exception says what happened and what fixes it: `Trip A123: appointment time is missing — enter a time to calculate pickup.`
- Permit valid rows to proceed; retain blocked rows as a visible work queue.
- On repeat import, summarize `new`, `changed by MTM`, `unchanged`, and `locally overridden`; never interpret absence as cancellation.
- Announce completion persistently: `48 trips imported. 3 still need review.` Alerts are appropriate for task status, validation, and confirmation. [USWDS: Alert](https://designsystem.digital.gov/components/alert/)

The upload control should be a progressively enhanced native file input with a visible label, accepted format, filename, and helpful error. USWDS recommends one file per input because some users do not know multi-select file dialogs. [USWDS: File input](https://designsystem.digital.gov/components/file-input/)

## 2. Pickup-time suggestions: fast by default, transparent on demand

The large-manifest workflow should be:

- Compute every eligible suggestion immediately after validation.
- Present one batch action: **Use 48 suggested pickup times**.
- Expand a short explanation beside the action: `Travel time + 15 min early arrival + 10 min loading`.
- Route uncertain cases to **Needs review**, for example missing/invalid address, missing appointment time, unusually long route, or mapping failure.
- After applying, show **Undo** and keep broker time, suggestion inputs, accepted value, overrides, actor, and timestamp in history.
- Never recompute an accepted/overridden time silently after a re-import or route-estimate refresh.

This preserves speed without disguising automation. Avoid a confirmation dialog for every row: WCAG's error-prevention explanation explicitly says its goal is not to require confirmation for every ordinary save. Favor reversible batch actions and focused review of consequential exceptions. [W3C: Error Prevention](https://www.w3.org/WAI/WCAG22/Understanding/error-prevention-legal-financial-data.html)

Do not introduce confidence percentages in the MVP unless they have a defensible meaning. Use actionable states instead: `Ready`, `Needs review`, `Cannot calculate`. When the user opens a time, show its concrete ingredients, not an opaque score.

## 3. Dispatcher day board: a work queue, not a spreadsheet clone

Keep the default row to information needed for the next decision:

`scheduled pickup | passenger | pickup → destination | service | driver | status/warning`

Put phone, notes, actual timestamps, MTM source values, overrides, and audit history in a side panel. This avoids the current site's wall of columns while preserving dense comparison. USWDS recommends tables for long, consistently structured lists, with brief cells, plain-language headers, predictable formatting, and sorting only where it is useful. It also recommends a sticky header for long tables and warns against using a table as a generic layout grid. [USWDS: Table](https://designsystem.digital.gov/components/table/)

Concrete behavior:

- Start on **Today**, sorted by scheduled pickup.
- Place persistent filter chips above the table: **Unassigned**, **Needs review**, **Not completed**, **Driver**. Always show active filters and a one-click **Clear filters**.
- Make the whole row selectable; open details without navigating away or losing scroll/filter position.
- Use a sticky header and freeze the time/passenger area if horizontal scrolling survives usability testing.
- Let the dispatcher select one or more **Journeys**, then choose **Assign driver**. One assignment applies to all uncompleted legs; an individual leg remains editable.
- Show selection explicitly: `3 journeys selected`; never place an unlabeled icon-only batch control in a distant toolbar.
- Warn, but do not block, when outbound and return legs have different drivers.
- Preserve the user's filters, date, and scroll position after edits.
- Avoid an “everything dashboard” of charts. The home view should answer: `What needs my attention today?`

Use text plus shape/icon for states; do not encode status only by red/green color. Repeated functions must retain the same name and placement across screens to reduce learning and cognitive load. [W3C: Consistent Identification](https://www.w3.org/WAI/WCAG22/Understanding/consistent-identification), [W3C: Consistent Navigation](https://www.w3.org/WAI/WCAG22/Understanding/consistent-navigation.html)

## 4. Driver PWA: one obvious next action

A driver should land on **My trips today**, not a dashboard. Each journey card should show:

- pickup time and status
- passenger name and only the service/mobility information needed for the ride
- pickup and destination with separate **Navigate** actions
- **Call passenger**
- the next valid state action: **Picked up** or **Dropped off**
- a secondary **Could not complete** action that requires a standardized reason and optional note

After a tap, immediately record the device capture time, change the card state, and offer a short **Undo** window. Do not make the driver type a time in the normal path. A correction can expose manual time entry and require a reason.

Make primary sequential controls at least 44 by 44 CSS pixels. WCAG 2.2 requires at least 24 by 24 CSS pixels (or sufficient spacing) at Level AA and recommends the larger 44-pixel target at its enhanced level, especially for frequent, sequential, or hard-to-undo actions. [W3C: Target Size Minimum](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum), [W3C: Target Size Enhanced](https://www.w3.org/WAI/WCAG22/Understanding/target-size-enhanced)

Always label icons (`Call passenger`, not a phone glyph alone). Give form controls persistent visible labels and format hints; placeholders are not labels. W3C notes that labels and instructions reduce incorrect submissions, particularly for users with cognitive, language, or learning disabilities. [W3C: Labels or Instructions](https://www.w3.org/WAI/WCAG22/Understanding/labels-or-instructions.html)

## 5. Offline and sync states are part of the workflow

A PWA can be installed from supporting browsers without packaging an app for an app store, though install behavior varies by browser/platform. It must still work as a normal website when installation is unavailable. [MDN: Making PWAs installable](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Making_PWAs_installable)

For the MVP, cache only the authenticated driver's limited current-day assignment data and queue only the operational actions needed offline. The interface must distinguish:

- **Saved** — acknowledged by the server
- **Waiting to sync** — safely stored on this device
- **Needs attention** — server rejected the action or a conflict exists

Show a persistent offline banner and a per-action sync marker. Never show “Saved” for a merely queued action. Service workers enable offline experiences, but browser lifecycle behavior means implementation must tolerate workers stopping and restarting; correctness cannot rely on a continuously running background process. [MDN: Offline and background operation](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Offline_and_background_operation), [web.dev: Service worker mindset](https://web.dev/articles/service-worker-mindset)

When a queued action conflicts with a cancellation or reassignment, retain both facts, show the captured device time separately from server receipt time, and send it to dispatcher review. The driver should receive a plain result: `Pickup saved; dispatcher review needed`, not a technical synchronization error.

## 6. Bilingual readiness without committing Arabic to MVP

Arabic translation may be deferred, but localization readiness is cheap only if designed in now:

- Put every interface string and standardized reason/status in localization resources; no concatenated UI sentences.
- Store stable status codes independently from translated labels.
- Use flexible layouts and CSS logical properties; do not encode meaning as “left” and “right.”
- Declare the page language and mark passages in another language with `lang`. [W3C: Declaring language in HTML](https://www.w3.org/International/questions/qa-html-language-declarations.html)
- When Arabic is added, set the document base direction with `dir="rtl"`; use `dir="auto"` or `<bdi>` for imported/user-entered strings where direction is unknown. Arabic layouts run RTL while numbers and embedded Latin strings may remain LTR, so trip IDs, times, phone numbers, and addresses need explicit mixed-direction testing. [W3C: Arabic and Persian Layout Requirements](https://www.w3.org/International/alreq/), [W3C: RTL HTML tutorial](https://www.w3.org/International/tutorials/bidi-xhtml/Overview.en)
- Translate interface chrome and controlled operational labels first. Do not machine-translate broker notes or safety-sensitive free text by default.
- Display the language choice in its own language (`English`, `العربية`) and remember it per user.

## 7. MVP usability checks and acceptance criteria

The following are product hypotheses and should be tested with the owner, son, and at least two drivers on their real devices before cutover:

1. Import a representative large manifest and reach a usable day schedule without editing the CSV.
2. Apply all ready pickup-time suggestions in one action, resolve each exception, override one time, and undo the batch.
3. Assign an entire journey, then reassign only its return leg without losing context.
4. Find every unassigned trip and every trip needing review using visible filters.
5. As a driver, locate the next pickup, open navigation, record pickup and drop-off, and correct a mistaken tap.
6. Repeat the driver flow while offline, restart the PWA, reconnect, and verify every queued action reaches an explicit final state.
7. Re-import a changed manifest and correctly distinguish broker changes from local overrides.
8. At 200% browser zoom and keyboard-only navigation, complete the dispatcher core flow without hidden controls or loss of information. WCAG requires content to support text resizing and reflow constraints rather than assuming a fixed viewport. [W3C: Resize Text](https://www.w3.org/WAI/WCAG22/Understanding/resize-text.html), [WCAG 2.2](https://www.w3.org/TR/WCAG22/)

Record task completion, time, wrong turns, errors requiring help, and the user's words. A useful MVP gate is not “users liked it”; it is that the client completes the end-to-end daily workflow without spreadsheet manipulation, no trip/timestamp is lost, and help requests decline over the three parallel-run days.

## MVP exclusions supported by this UX direction

- No automatic driver assignment or route optimization.
- No app-store distribution.
- No per-row approval of normal pickup-time suggestions.
- No charts-first management dashboard.
- No free-form status taxonomy.
- No automatic translation of imported or safety-sensitive text.
- No hidden background success: every offline action exposes its sync state.

