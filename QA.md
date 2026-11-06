# Phase 1 verification

Executed on 5 November 2026. CampusCore is a fictional, local, read-only portfolio demo.

| Check | Result | Evidence |
|---|---|---|
| SDK/environment | PASS | SDK 10.0.403; ASP.NET Core runtime 10.0.14; Git 2.56.0.windows.4 |
| dotnet restore | PASS | All project dependencies restored; no external PackageReference dependencies |
| dotnet build | PASS | Final build: 0 warnings, 0 errors |
| App startup | PASS | Kestrel listening on http://localhost:5080 in Development |
| Dataset and query checks | PASS | 22 checks via --verify-data |
| HTTP route checks | PASS | 24 explicit checks including assets, filters, friendly 404/500 and rejected POST (405) |
| Local link crawl | PASS | All 248 discovered URLs returned 200, including all directories, pagination and linked details |
| Dashboard reconciliation | PASS | 48 students; 11 active courses; 158 enrollments; 107 / 194 occupied seats; rounded utilization 55% |
| Search/filter/sort/pagination | PASS | Service-level checks, rendered HTTP checks and browser form interactions |
| JavaScript syntax | PASS | node --check wwwroot/js/site.js |
| Browser visual review | PASS | Desktop, tablet and mobile screenshots actually reviewed in the in-app browser |
| Responsive overflow | PASS | All 7 page types checked at 1440, 768, 390 and 320 pixels: 28 / 28 fit the viewport |
| Keyboard basics | PASS | Skip link focuses main; Escape closes navigation and returns focus to toggle |
| Browser console | PASS | No warning/error entries returned during final browser interactions |
| Real devices / other browsers | NOT TESTED | Checks used the available desktop in-app browser with viewport overrides |
| Full WCAG/screen reader audit | NOT TESTED | Labels, semantic headings, focus, skip link and keyboard behavior reviewed; no certification claimed |
| EF Core / SQL Server / persisted CRUD | NOT TESTED | Deliberately not implemented in Phase 1 |

## Reproduce

Use the .NET 10 SDK. Run the following inside this project folder:

```powershell
dotnet restore
dotnet build
dotnet run --no-launch-profile -- --verify-data
dotnet run --launch-profile CampusCore
```

In a second PowerShell 7 terminal, run:

```powershell
./scripts/verify-http.ps1
```

The HTTP script is localhost-only. The source-integrity checks are implemented in Services/DemoVerification.cs and exercise the same query functions used by MVC directories. Browser checks were performed interactively and are not an automated browser test suite.

## Resolved findings

- The first build found Razor treating SVG text as Razor text blocks. Grouped the SVG elements correctly; subsequent builds pass.
- Responsive QA found tablet enrollment overflow. Added responsive record cards and positioned the table wrapper; the full final viewport matrix passes.
- Increased secondary text contrast and small-screen chart labels. Enrollment histories include record IDs and terms so historical/current records can be distinguished.
- The initial restricted dotnet --info workload inspection hit service-control permissions. Repeating it outside that restriction succeeded. This existing Codex session has an older PATH, so checks used the installed dotnet executable. No software was installed during implementation; the SDK template performed its normal first-run development-certificate setup.

## Source map

- CampusCore.csproj, global.json, Program.cs: framework selection, services, read-only routes and error handling.
- Controllers/HomeController.cs and Controllers/DirectoriesController.cs: overview, directories and details.
- Models/AcademicModels.cs: students, courses, enrollments and presentation models.
- Services/AcademicData.cs: immutable centralized demo data, relationship projections and query filtering.
- Services/DemoVerification.cs: repeatable dataset/query checks.
- Views/Home, Views/Academic, Views/Shared: Razor screens and reusable layout/record partial.
- wwwroot/css/site.css, wwwroot/js/site.js, wwwroot/icons.svg, wwwroot/favicon.svg: local presentation assets.
- wwwroot/lib/bootstrap: Bootstrap 5.3.3 and its bundled license.
- Properties/launchSettings.json: localhost:5080 launch profile.
- appsettings.json, appsettings.Development.json: template logging settings; no database connection.
- scripts/verify-http.ps1: repeatable HTTP and link checks.
- README.md, QA.md, .gitignore: project instructions, verification record and generated-file exclusions.
- bin and obj: generated build outputs, excluded from Git.

## Limits and next phase

The Fall 2026 dataset is deterministic and fictional. CRUD, authentication, deployment and database integration are intentionally absent. IAcademicData provides the read boundary for Phase 2; the README describes EF Core/SQL Server integration, validation, transactional capacity checks, enrollment uniqueness, antiforgery-protected writes and referential integrity work still required. No commits, pushes or deployment were performed, and no existing ApplyFlow or ReportFlow files were touched.

## Final targeted Phase 1 polish — 5 November 2026

Preserved the navy/teal identity, dashboard grid, cards, charts, data and read-only behavior. No packages or new product features were added.

Changes:
- Views/Shared/_Layout.cshtml: clarified the primary destination as Dashboard, named the primary navigation landmark, added visible Menu text, and hid the redundant Workspace breadcrumb on narrow phones to keep the header clear.
- Views/Home/Index.cshtml: aligned the browser title with Dashboard; chart calculations and geometry remain unchanged.
- Views/Academic/Directory.cshtml: made the table region keyboard-focusable and gave responsive capacity/course-load/result labels the same meaning as their table headings.
- wwwroot/js/site.js: fixed the confirmed mobile keyboard-order issue by focusing the current navigation link when opening the menu. Closing navigation restores focus when needed; Escape remains supported.
- wwwroot/css/site.css: increased operational secondary text from 8–10px to mostly 11–13px, enlarged controls and detail-link targets, increased mobile SVG label sizes to account for scaling, and darkened low-contrast soft labels/detail arrows within the existing palette.
- Controllers/DirectoriesController.cs: withdrawn enrollment details now show Not applicable for an absent grade, matching the directory.

Final executed results:
- PASS dotnet build --no-restore: zero warnings and zero errors, SDK 10.0.403 / net10.0.
- PASS all 22 data/query consistency checks; PASS JavaScript syntax check.
- PASS existing 24 HTTP checks, all 248 discovered local URLs, plus a withdrawn-record grade-label assertion.
- PASS browser searches, combined department/status filters, empty states and clear-filter recovery for Students, Courses and Enrollments.
- PASS all four sort options in all three directories (12 browser checks).
- PASS enrollment term/year filtering and pagination retaining department, status, term/year and sort.
- PASS linked details and the archived-course empty enrollment history.
- PASS all 35 page/viewport combinations across Dashboard, the three directories and three detail types, at 1440, 1024, 768, 390 and 320 pixels. No page overflow, missing form labels or duplicate main headings found.
- PASS actual desktop, tablet and phone screenshot reviews.
- PASS mobile keyboard opening to the active link, Tab to Students, Enter navigation, Escape closing with focus return, and skip link focusing main.
- PASS sampled adjusted label and arrow text contrast: 5.16:1–5.49:1 against rendered backgrounds. This is a targeted check, not a full accessibility certification.
- PASS no warning/error entries returned by the browser console during final interactions.
- PASS unchanged metrics: 48 students, 11 active courses, 158 enrollments, 107 occupied / 194 available seats (55%). Trend counts: Jan 16, Feb 16, Mar 16, Aug 36, Sep 36, Oct 38. Each department: 12 students (25%).
- NOT TESTED real devices, other browser engines and a full screen-reader/WCAG audit.
- NOT TESTED SQL Server, EF Core or persistence: not implemented in Phase 1.

Phase 1 is ready as the foundation for EF Core + SQL Server integration, with no critical blockers identified in these checks. Database schema/migrations, integrity constraints, validation, transactional capacity handling and protected write operations remain Phase 2 work. No SQL Server installation, authentication, write workflows, commits, pushes or deployment were performed. The local launch profile remains http://localhost:5080.


## Phase 2 — SQL Server integration and real CRUD

Executed 5 November 2026. The earlier Phase 1 sections above remain a historical record; this section describes the current SQL-backed application.

| Check | Result | Actual evidence |
|---|---|---|
| Environment and SQL connectivity | PASS | LocalDB 17.0.1000.7 Running; real Windows-authenticated SELECT from master |
| Restore/build | PASS | .NET SDK 10.0.403; final builds have 0 warnings and 0 errors |
| EF packages/tooling | PASS | SQL Server / Design / local dotnet-ef 10.0.14; no additional system software installed |
| Migration | PASS | Reviewed create-only InitialAcademicSchema applied to confirmed previously absent CampusCoreDevelopment; matching model/snapshot; idempotent SQL generated |
| Seed | PASS | Explicit development seed inserted 48/12/158; second invocation skipped existing data; isolated tests proved existing edits are preserved |
| SQL integration and policies | PASS | 53 checks on a uniquely named disposable LocalDB database; only that test database was cleaned up |
| Baseline queries/metrics | PASS | 22 checks against actual SQL records |
| Student CRUD | PASS | HTTP create/edit/delete, duplicate validation, stale-form rejection and dependency deletion block |
| Course CRUD | PASS | HTTP create/edit/delete, duplicate/code/capacity validation and dependency deletion block |
| Enrollment management | PASS | HTTP create/update/withdraw/remove and duplicate/capacity rejection; SQL tests also recorded/corrected grades and retained completed history |
| Concurrent capacity checks | PASS | Two real SQL contexts raced for one remaining seat; exactly one succeeded |
| Persistence after restart | PASS | Actual application process stopped/restarted; student edit, course edit, enrollment and changed dashboard values remained saved |
| Live dashboard | PASS | HTTP QA records changed counts to 49 students, 12 active courses, 159 enrollments, 108/195 seats and added Nov registrations; cleanup restored original data |
| HTTP CRUD/validation | PASS | 26 setup checks, 4 restart checks, 6 cleanup checks; QA records removed via guarded application forms |
| Antiforgery/validation | PASS | Missing and invalid tokens return 400; invalid forms return 422; duplicate/stale forms rejected |
| Hosted/disabled write rejection | PASS | Two actual localhost servers: Production with flag true and Development with flag false; 10 HTTP checks each rejected write forms and POST/PUT/DELETE while permitting reads |
| Public/proxied/cross-origin write rejection | PASS | Actual Host/forwarded/Origin HTTP requests rejected; controlled policy tests also reject non-loopback client addresses |
| Schema integrity | PASS | Actual SQL rejects duplicate numbers/codes/enrollments, missing FK, invalid capacity and restricted parent deletion; rowversion conflicts tested |
| Read routes/link crawl | PASS | 24 explicit smoke checks; all 687 discovered GET links returned 200, including create/edit/delete confirmation screens |
| Search/sort/filter/empty states | PASS | SQL-backed browser checks for all three modules, all four sort choices, combined filters and clear-filter recovery; term/pagination covered by HTTP/service checks |
| Responsive/forms/navigation | PASS | 80 page/viewport combinations: 16 page types at 1440/1024/768/390/320px; no page overflow, missing field labels, duplicate main headings or missing active section |
| Keyboard and visual review | PASS | Real desktop/tablet/phone screenshots; menu opens to active section, Escape returns focus, skip link focuses main |
| JavaScript / console | PASS | Syntax check passed; final browser interactions returned no warning/error entries |
| Dependency audit | PASS | No vulnerable application packages reported after direct Pkcs 10.0.14 override |
| Physical devices / other browser engines / full WCAG audit | NOT TESTED | Available desktop in-app browser and viewport overrides used |
| External network penetration / production deployment | NOT TESTED | No public server was exposed or deployed; public write checks used controlled local requests/policy contexts |
| Backups, production authentication and operational readiness | NOT TESTED | Outside Phase 2 |

### Final seeded metrics

48 students; 11 active of 12 courses; 158 enrollments; 107 enrolled seats / 194 active capacity = 55% rounded. Registration months: January 16, February 16, March 16, August 36, September 36, October 38. Each department has 12 students / 25%. No hardcoded KPI fallback is used.

### Resolved findings during Phase 2

- NuGet initially reported a vulnerable transitive System.Security.Cryptography.Pkcs 9.0.11 dependency. Explicit 10.0.14 pin resolved the application audit and build warnings.
- HTTP testing caught POST error re-execution changing antiforgery 400 to 405. The non-mutating error/status actions now accept re-execution and skip antiforgery themselves; the write gate still precedes MVC.
- MVC initially inferred a required non-empty Version on new forms. Empty-string version binding is now preserved for creation; service checks still require matching versions for edits/deletes. Actual create/stale-form tests pass.
- Valid single-character course codes now render safely; a real SQL regression test covers this edge case.
- Existing completed history cannot be withdrawn to bypass deletion restrictions. SQL integration checks prove the retained state and editable grade.
- Form pages now select the correct Students/Courses/Enrollments navigation item. Menu focus now selects the current section rather than the first link in a combined selector.

### Changed files and limits

Added Data context/safety/seed/snapshot/migrations, persistent entity and form models, transactional write service, local-write policy/filter, Management controller and three forms plus delete confirmation, SQL integration tests and HTTP CRUD/security script. Updated Program/configuration, project/local tool dependencies, shared layout, directories/details/dashboard/status views, form CSS, navigation JS, legacy smoke script, README and this QA record. Existing C# was whitespace-formatted. No ApplyFlow/ReportFlow files were changed.

The local database files live outside the OneDrive project under LocalDB's user profile storage. No existing database was overwritten; no Down migration or reset was run. There is no auto-migration or auto-seed on web startup. No credentials, authentication, paid services, public repository, commit, push or deployment were introduced. Temporary verification servers on 5081/5082 were stopped; the development app is left on http://localhost:5080.

No critical Phase 2 blockers remain. Phase 3 can start from this verified CRUD foundation, but public writable hosting remains prohibited until authentication/authorization and production controls are separately designed, implemented and tested. Request snapshots are intentionally simple for this small local dataset; large-scale database paging is future work.

## Phase 3 final verification — 6 November 2026

Resumed the existing implementation and inspected unfinished files and published output. CampusCore has no Git repository, so a Git diff/status was unavailable; no repository was initialized. Release and published DLL hashes matched. No application source rewrite occurred in the resumed final verification.

| Check | Result | Actual evidence |
| --- | --- | --- |
| Release restore/build | PASS | .NET SDK 10.0.403; zero warnings and errors |
| Dependency audit | PASS | No vulnerable packages, including transitive dependencies |
| Published static delivery | PASS | CSS 200 text/css 24,567 bytes; Bootstrap 200 text/css 232,803 bytes; JS 200 text/javascript 1,439 bytes |
| Production published HTTP suite | PASS | 164 assertions and 248 linked GET URLs; published DLL and physical wwwroot |
| PublicDemo without SQL | PASS | Cold run with LocalDB stopped before and after; 7 DI isolation checks and 22 dataset queries; invalid SQL configuration and writes=true cannot enable database services |
| PublicDemo mutation denial | PASS | Unsafe methods rejected; write form routes unavailable; no mutation controls; migration/seed/database commands refused |
| Invalid configuration | PASS | 8 bounded startup/command rejection cases |
| SQL relationships and security | PASS | 53 database/security checks plus 22 dataset queries in isolated fixture |
| Student/course/enrollment HTTP CRUD | PASS | 26 setup assertions, process restart, 4 persistence assertions, 6 cleanup assertions |
| Enrollment validation and retention | PASS | 16 checks including grade correction/retention, duplicate email, capacity/credits, and four nonlocal/proxy/origin read denials |
| SQL chart edge cases | PASS | 15 HTTP checks; empty and single-month SVG finite and truthful; temporary records removed |
| Metrics/charts | PASS | 48 students, 11 active of 12 courses, 158 registrations; 107/194 seats rounds to 55%; four departments each 12/25%; monthly 16,16,16,36,36,38 |
| Actual browser and responsive UI | PASS | 115 page/viewport combinations across 1440,1024,768,390,320 pixels; published public directories/details and SQL forms/details; no horizontal overflow or heading/label/navigation failures |
| Browser interactions | PASS | All three directories: search, descending sorting, department filters and empty states; mobile menu, Escape and keyboard skip navigation; styled dashboard and charts |
| Accessibility basics | PASS | One primary heading, labelled forms, current section navigation, visible keyboard focus; targeted contrast checks passed; mobile badge increased to readable 10px |
| LocalSql read protection | PASS | Loopback restrictions apply to reads and writes; forwarded/original headers and foreign Origin refused; Production LocalSql startup refused |
| Antiforgery/concurrency/data protection | PASS | Real HTTP and SQL regressions cover missing antiforgery, stale versions, constraints and history preservation; development-data fingerprints include all fields and rowversions |
| Physical devices / screen reader / full WCAG | NOT TESTED | Desktop browser viewport checks and targeted accessibility checks only |
| External penetration / real production hosting | NOT TESTED | No deployment or external exposure; provider TLS/domain/CDN/proxy behavior requires Phase 4 validation |

### Genuine Phase 3 fixes and changed areas

Program/configuration now select explicit fail-closed modes and register only the relevant services. CampusCoreMode and DemoModeVerification were added. LocalWriteAccess and DatabaseSafety enforce mode and loopback restrictions. Home error/status handling clamps invalid status input. Public layout/directories/details display fictional read-only context and suppress write affordances and success notices. Dashboard empty/single-month chart states avoid non-finite labels or misleading filled areas. Cache headers match antiforgery no-store behavior. Static asset delivery uses UseStaticFiles before routing with physical published wwwroot. Mobile badge text is 10px. The established colors, layout, cards and normal chart design are preserved.

Changed areas: Program.cs; Configuration/CampusCoreMode.cs; Security/LocalWriteAccess.cs; Data/DatabaseSafety.cs; Controllers/HomeController.cs; Services/DemoModeVerification.cs and AcademicData comment; appsettings files; shared imports/layout and directory/detail/dashboard views; site.css; four focused verification scripts; README.md and QA.md. Existing SQL entities/schema/write architecture remain intact. No new Phase 3 packages, authentication or system software were installed.

The two task-owned disposable SQL databases were removed after their servers stopped. The original development database is preserved; final all-field/rowversion hashes are compared with both the pre-Phase-3 and resumed baselines. Historical Phase 2 server statements above describe that earlier checkpoint; current review endpoint is the published Production PublicDemo at http://localhost:5090.

No critical application blocker remains for Phase 4 planning. Hosting-specific validation remains outstanding; this is not a deployment readiness claim for an unselected provider. No commit, push or deploy occurred. No reset, destructive migration or modification of existing development records was performed. Stop after Phase 3 verification.

## Phase 4A independent final audit — 6 November 2026

Fresh evidence and limitations are recorded in [AUDIT.md](AUDIT.md). Release build/publish and audit passed; 164 demo HTTP checks plus 248 links; 21 responsive browser combinations; 8 configuration failures; 7+22 demo isolation/query checks; 53+22 SQL/security/query checks; HTTP CRUD 26 setup, 4 actual-restart persistence, 6 cleanup checks. All existing academic field/rowversion fingerprints unchanged. Owned temporary SQL databases removed. No application source changes; publication documentation and ignore coverage updated. Git tracked/history review is not possible before repository initialization. Actual external hosting and complete accessibility/penetration audits remain NOT TESTED. No commit, push, install or deploy.


## Phase 4B preparation checkpoint — 6 November 2026

Local Git repository initialized on main, with no commit or remote. Genuine published PublicDemo screenshots added under docs/screenshots and README updated with implemented features, stack, safe modes/setup, validation, test evidence and undeployed status. Historical Phase 4A statements above describe the earlier no-repository checkpoint. License approval remains pending; no LICENSE added. Source/application design and database records unchanged. Staged-file review and clean-export test are recorded in PUBLICATION.md.

Phase 4B clean staged export: tool/package restore, Release build (0 warnings/errors), publish, 7+22 demo isolation/query checks, 164 HTTP assertions and 248 links PASS. No SQL data writes. Final approval packet and complete staged inventory: PUBLICATION.md.
