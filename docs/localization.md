# English and Arabic

MDSweep's Angular interface supports English and Modern Standard Arabic. English is the default, even when the browser prefers another language. The language selector appears beside Theme in the user menu and on Angular access/recovery screens. The preference is stored under `mdsweep.language` in browser local storage, like the theme; it is not an account preference and does not synchronize across devices.

Arabic uses RTL layout, Gregorian dates, western `0–9` digits, and the existing 12-hour clock presentation. Language changes affect presentation only: service dates, wall-clock times, time zones, API role/status codes, broker facts, imported notes, and authorization retain their meanings. Unknown broker values are displayed as received. Keycloak pages and invitation emails remain outside this release.

## Translation sources

Edit `src/Mdsweep.Web/public/i18n/en.json` and `ar.json`. Both catalogs use the same feature-grouped keys. Use complete ICU messages with named parameters instead of concatenating translated fragments. Use all applicable Arabic plural categories; interpolate `{count}` in Arabic plural branches to preserve western digits. Imported identifiers embedded in messages use directional isolation, and templates use `bdi` for imported/user-entered text.

`npm run i18n:compile` validates matching keys and parameters and compiles ICU messages into ignored modules under `src/app/core/i18n/generated`. The normal start, build, test, and watch scripts run this step automatically. When editing catalogs during an already-running dev session, run `npm run i18n:compile` again. Compile before invoking `ng` directly. Do not edit generated modules.

Both compiled catalogs ship with the application. This makes switching synchronous, including offline, and avoids shipping the MessageFormat compiler to browsers. The service worker caches the application JavaScript; the source JSON files are not loaded or separately cached at runtime. `LanguageService` restores preferences and synchronizes document/CDK direction. Calendar localization stays in the lazy Trips feature so calendar dependencies are not pulled into bootstrap.

## API and UI boundary

UI primitives under `src/app/ui` do not import application translation services or reference catalog keys. Pass translated accessible labels from application components through plain inputs such as `closeLabel`; dialog/sheet close labels default to English `Close`. Programmatic dialogs accept the label through their options.

`LanguageService.formatDate` accepts `Date` values only. Convert known API timestamps explicitly at their consuming component. Calendar dates such as `yyyy-MM-dd` must be constructed from calendar fields, never parsed with `new Date(string)`, which interprets date-only strings as UTC and can shift the displayed day.

Translation is a frontend concern. API responses retain English diagnostic messages. Generic HTTP failures and ordinary input validation do not require application error codes. Add a stable code only for a server-authoritative business condition when Angular needs a distinct localized message or behavior that cannot be inferred from HTTP status alone. Keep codes beside the conditions that produce them; extract constants only when real repetition justifies it.

FluentValidation continues enforcing all request rules on the server and returns Wolverine's standard validation ProblemDetails. Angular validates form inputs locally and displays translated messages. Unexpected server validation failures use localized guidance to check the entered values; the English field diagnostics remain available in the response. Client validation does not replace server validation.

Business validation responses preserve the existing `errors` dictionary and optionally add `issues` with `field` and `code` values (for example, `membershipExists` or `invitationEmailMismatch`). Uncoded validation results do not acquire a generic business code. Import problems retain row/Trip/field information and their original message, plus feature-local `manifest.*` codes and optional parameters, because the parser alone knows those failures.

`ApplicationError` contains transport data: status, English diagnostics, validation errors, and `ApiIssue` records. `httpErrorMessage()` converts issues to frontend `{ key, params }` descriptors by convention (`membershipExists` becomes `errors.membershipExists`), maps generic HTTP failures, and otherwise uses the caller's recovery message. Unknown translation keys display a localized generic failure; never match English sentences to keys.

UI feedback retains these descriptors until display so an already-visible error changes language without rerunning the request. Multiple business issues use a flat list of messages, not a recursive error tree. Read `issue.code` when a business condition requires distinct behavior, such as offering an account switch for an invitation email mismatch.

## Arabic glossary for review

These are the initial terms used in the interface; refine the Arabic values without changing domain identifiers or API codes.

| English domain term | Arabic |
| --- | --- |
| Tenant | جهة النقل |
| User | المستخدم |
| Administrator | مسؤول النظام |
| Dispatcher | منسق الرحلات |
| Driver | السائق |
| Passenger | الراكب |
| Trip | الرحلة |
| Trip Import | استيراد الرحلات |
| Manifest | ملف الرحلات |
| Invitation | الدعوة |
| Assignment | الإسناد |
| Vehicle | المركبة |
| Scheduled pickup | وقت الاصطحاب المجدول |
| Will-call | عند الاتصال |
| Broker | وسيط النقل |

## Verification

Run the standard .NET and Angular checks, then `npm run test:e2e -- --workers=1` in the web project. Localization coverage includes switching without losing state, translated server feedback, Arabic calendar navigation, mixed-direction values, mobile/desktop layout, and accessibility in light/dark themes.

Run `npm run test:e2e:pwa -- --workers=1` for the production service-worker test. It builds the app and serves it on a loopback-only test server. The test installs the PWA, switches languages offline, and reloads into the localized session-recovery screen. This does not add an offline session cache or a Driver workflow. All fixtures and screenshots are synthetic.
