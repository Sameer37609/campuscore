# Phase 4A audit — 6 November 2026

## Decision

GO for source publication preparation; no critical application vulnerability was confirmed. NO-GO for immediate unreviewed upload or deployment: no Git index/history exists to certify, the eventual staged file set must be reviewed, and hosting HTTPS/proxy/domain configuration is not selected or tested. No app source changes were warranted. This is an audit, not a penetration-test certificate.

## Fresh evidence

- Release build: PASS, zero warnings/errors. Local framework-dependent publish: PASS in the external phase4a-publish directory.
- Transitive NuGet audit: PASS, none reported by the configured NuGet source. This does not certify vendored frontend dependencies or future advisories.
- Configuration failure tests: PASS, 8 cases.
- Published PublicDemo started in Production with invalid SQL connection text and writes=true: PASS. DI verification: 7 isolation checks + 22 fictional query checks; no SQL context/write service registered.
- Published demo HTTP: PASS, 164 checks + 248 linked GET URLs. Correct nonempty CSS/Bootstrap/JS/SVG, all read modules/details, search/sorts/filters/empty states, fixture metrics/chart totals, private-file 404s, error disclosure and unsafe-method denials.
- Actual published browser: PASS, styled dashboard/charts observed; 21 combinations of 7 pages at 1440/768/390 pixels, no overflow, exactly one h1 and no unlabelled visible input/select controls. This is targeted accessibility evidence only.
- SQL integration/security: PASS, 53 checks + 22 queries in a disposable database, including relationships, uniqueness, validation, capacity concurrency, rowversion, history retention, persistence and local access policy.
- Fresh HTTP SQL fixture: PASS, 26 setup assertions covering antiforgery/validation/CRUD/stale forms, followed by actual process restart, 4 persistence checks and 6 cleanup checks. Disposable fixture database removed afterward.
- Existing CampusCoreDevelopment: PASS, all-field/rowversion SHA256 fingerprints for Students, Courses and Enrollments match the prior Phase 3 baseline. No existing record changes.
- SQL engine physically stopped during this phase: NOT TESTED; Phase 3 cold-engine test passed. Fresh DI isolation with invalid SQL configuration independently proves no SQL service initialization.
- Physical devices, screen readers, full WCAG, other browser engines, external penetration test and actual hosting TLS/proxy behavior: NOT TESTED.

## Source and security review

Simple MVC/query-snapshot/write-service structure is suitable for this small portfolio dataset. EF constraints, restricted deletes, rowversions and transactional serialized writes provide relationship and capacity integrity. All mutation controllers use the local authorization filter; automatic antiforgery applies globally. Mode gate runs before MVC/static routing and forbids unsafe demo requests. LocalSql refuses Production and rejects nonloopback addresses/Host, forwarded/original headers and foreign Origin. It authorizes a trusted local machine, not individual users; do not expose it through a local relay or tunnel.

Razor encoding, form models and parameterized EF access were reviewed; no unsafe raw HTML rendering or concatenated application SQL from user input was found. Error pages avoid implementation details; database write failures return safe messages. Public static serving exposes only wwwroot, not source/configuration. PublicDemo cannot carry local records across requests or initialize SQL dependencies. Development local integrated connection is non-secret and not served by HTTP. No passwords/private keys/database files were found in the candidate source tree outside ignored build output. This is a targeted file/pattern review, not a dedicated secret-scanner certification.

There is no .git directory or Git parent repository: tracked file/history audit is NOT TESTED/not applicable until initialization. Ignored bin/obj contain machine-generated QA material and must never be uploaded manually. Initial .gitignore lacked database/key/secret coverage; it now excludes these plus published/test output. Exclusions are preventive, not evidence that an eventual Git index is safe.

Maintainability observations, nonblocking: some controllers/write methods use dense one-line statements; snapshot queries load whole datasets; verification code lives in the web assembly; Windows-specific QA paths and fixed fixture assumptions reduce portability. No architecture rewrite or extra packages were justified. App dependencies are narrow; vendored Bootstrap license exists. No project LICENSE exists; MIT is a recommendation awaiting the owner, not an automatic license grant.

## Hosting boundary

Current source has no explicit HTTPS redirection/HSTS or trusted forwarded-header setup. Before exposure, configure and test edge HTTPS enforcement/HSTS, approved hostnames, restricted backend networking and any needed trusted proxy handling. Do not claim local HTTP checks certify this. Use Production/PublicDemo, writes=false, no SQL config. Full physical wwwroot output and ASP.NET Core 10 runtime are required. .NET 10 is LTS; verify current patched runtime before deployment. Linux/container and IIS target-specific execution remain untested. Free Docker hosting is plausible for a sleeping/quota-limited portfolio preview, not a production SLA; no service is selected or purchased.

## Changes / next phases

Changed only .gitignore, README.md, QA.md and this AUDIT.md. README replaces stacked historical instructions with current safe portable mode/run/publish guidance and honest portfolio status. UI/application source unchanged. No software installed, Git initialized, commit/push, remote or deployment. ApplyFlow/ReportFlow untouched.

Phase 4B: choose license; capture approved actual desktop/mobile demo screenshots; initialize local repository only when authorized; inspect staged paths/diffs with the ignore rules, scan for secrets/generated/personal data, verify a clean clone setup, then request publication authorization. Suggested name campuscore; description in README.

Phase 4C: choose a .NET 10-compatible host without purchasing; prepare target-specific package/container if authorized; verify Linux/runtime/port binding, HTTPS/HSTS/Host/proxy/backend isolation, then repeat published demo/mutation-denial/browser checks in staging. Deploy only after explicit approval. Never deploy LocalSql or public write workflows.

## Phase 4B preparation checkpoint — 6 November 2026

Local Git repository initialized on main, with no commit or remote. Genuine published PublicDemo screenshots added under docs/screenshots and README updated with implemented features, stack, safe modes/setup, validation, test evidence and undeployed status. Historical Phase 4A statements above describe the earlier no-repository checkpoint. License approval remains pending; no LICENSE added. Source/application design and database records unchanged. Staged-file review and clean-export test are recorded in PUBLICATION.md.
