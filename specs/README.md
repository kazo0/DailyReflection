# DailyReflection Uno — Implementation Specs

These specs close the gaps identified in [`docs/ANALYSIS.md` §10](../docs/ANALYSIS.md) (deep gap analysis of the Uno port vs. the original Xamarin.Forms app).

Each spec is self‑contained: summary, gap references back to §10.x, acceptance criteria, implementation plan with concrete file paths, risks, and a "done when" checklist. Read the gap analysis first; the specs assume that context.

## Status (2026-05-03)

All 11 specs are **Implemented** — the implementation pass landed alongside the spec text. Each spec's "Done when" checklist is fully ticked, with manual verification items called out per spec where mobile / desktop runtime smoke tests still need to happen on real hardware.

## Index

| # | Spec | Severity | Status | Gaps closed | Depends on |
|---|---|---|---|---|---|
| [001](001-version-tracking-and-migrations.md) | Version tracking, settings & DB migrations, runtime app version | 🔴 | ✅ Implemented | 10.6.1, 10.6.2, 10.8.1, 10.8.2, 10.8.3, 10.8.4, 10.3.7 | — |
| [002](002-desktop-notifications.md) | Desktop notification path with graceful fallback | 🔴 | ✅ Implemented | 10.5.1, 10.3.2 | — |
| [003](003-reflection-html-rendering.md) | Reflection HTML emphasis + ProgressRing z‑order + first‑load semantics | 🔴 / 🟠 | ✅ Implemented | 10.1.1, 10.1.2, 10.1.3, 10.1.4 | — |
| [004](004-theme-and-shell-chrome.md) | Restore page background, label colour, tab‑bar chrome to original palette | 🔴 / 🟠 | ✅ Implemented | 10.4.1, 10.4.2, 10.4.3, 10.4.4, 10.4.5, 10.10.1, 10.10.2, 10.10.4 | — |
| [005](005-automation-ids.md) | Apply `AutomationProperties.AutomationId` everywhere `AutomationConstants` defines one | 🟡 | ✅ Implemented | 10.1.8, 10.2.4, 10.3.9, 10.11.2 | — |
| [006](006-settings-correctness.md) | Settings: messenger leak, time setter, Android tappable rule, page lifecycle, sober‑date converter parameter | 🔴 / 🟠 | ✅ Implemented | 10.2.1, 10.2.2, 10.2.5, 10.2.6, 10.3.1, 10.3.4, 10.3.5, 10.3.6, 10.3.8, 10.1.10 | 005 |
| [007](007-share-service-hardening.md) | `ShareService` race condition + unsupported‑platform fallback + handler lifetime | 🔴 | ✅ Implemented | 10.7.1, 10.7.2, 10.7.3, 10.7.4 | — |
| [008](008-notification-behavior-alignment.md) | iOS auth options, sound, identifiers, badge reset; Android wording, priority, exported flag, intent flags, channel sound | 🔴 / 🟠 | ✅ Implemented | 10.5.2, 10.5.3, 10.5.4, 10.5.5, 10.5.6, 10.5.7, 10.5.8, 10.5.9, 10.5.10, 10.5.11, 10.5.12, 10.5.13 | 002 |
| [009](009-android-permissions-and-manifest.md) | Drop extra Android permissions; make min/target SDK explicit | 🟠 / 🟡 | ✅ Implemented | 10.5.14, 10.9.6, 10.9.7 | — |
| [010](010-ios-info-plist-alignment.md) | Restore iPhone orientations, set minimum OS, decide launch storyboard story | 🟠 | ✅ Implemented | 10.9.2, 10.9.3, 10.9.4, 10.9.5 | — |
| [011](011-cleanup-tests-and-di.md) | Dead code removal, test‑TFM bump, UI test scaffold, constructor‑injection refactor, bundle id decision | 🟡 / 🟠 | ✅ Implemented | 10.1.10, 10.2.6, 10.10.3, 10.10.5, 10.11.1, 10.11.3, 10.8.5, 10.8.6, 10.8.7, 10.8.8, 10.9.1, 10.9.8 | 005 |

Total: 11 specs covering every numbered gap in §10.1–§10.11 of the analysis.

### Tests added during implementation

* `DailyReflection.Services.Tests/Startup/StartupMigrationRunnerTests.cs` — 7 scenarios covering version-gate predicates.
* `DailyReflection.Services.Tests/Extensions/HtmlInlineParserTests.cs` — 10 scenarios covering the inline-markup parser.
* `DailyReflection.Services.Tests/AutomationConstantsCoverageTests.cs` — lint test asserting every constant is referenced by at least one Uno view.
* `DailyReflection.Services.Tests/Views/ViewSurfaceTests.cs` — 5 XAML-shape assertions (binding contract, z-order, theme brushes).
* `DailyReflection.Presentation.Tests/MessagePropagationTests.cs` — 2 scenarios covering message propagation on active vs. inactive view-models.
* `DailyReflection.Presentation.Tests/ShareClosureCaptureTests.cs` — 1 scenario covering the share closure-capture invariant.
* Existing VM tests modernised to NUnit 4 constraint syntax + the actual VM API; `SettingsViewModel` regressions (date rebase, missing persistence, MaxDate type) fixed along the way.

**Total**: 25 service tests + 23 presentation tests = 48 tests, all green on `net10.0`.

## Severity legend

* 🔴 **Functional gap** — feature missing or broken vs. the Xamarin original. Must‑do for parity.
* 🟠 **Behavioural drift** — feature works but differs in observable behaviour, persistence, or user experience.
* 🟡 **Quality / parity issue** — code‑style, testability, or porting cleanliness; not user‑facing.

## Recommended sequencing

```
001 (migrations) ─┐
002 (desktop notif) ─┼─► 008 (notif alignment) ─┐
003 (HTML render) ─┤                            │
004 (theme/chrome) ─┤                            ├─► 011 (cleanup/tests/DI)
005 (automation ids) ─┼─► 006 (settings)        │
007 (share) ─┤                                  │
009 (android perms) ─┤                          │
010 (ios plist) ─────────────────────────────────┘
```

Specs 001–005 and 007/009/010 are independent; pick them up in parallel. 006 lands more cleanly after 005 (so the new automation IDs are added once). 008 inherits the desktop work from 002. 011 should close last so dead code and TFM bumps don't churn the other PRs.

## Conventions

* **Paths.** All file paths in specs are absolute from the repo root (`heads/DailyReflection.Uno/...`, `DailyReflection.Presentation/...`). The Xamarin original at `heads/DailyReflection.Xamarin/` is read‑only reference; do not modify it.
* **Out of scope guardrail.** No spec touches the MAUI head (`heads/DailyReflection.Maui/`) or the Avalonia head unless explicitly stated. Changes to the shared layers (`DailyReflection.{Core,Data,Services,Presentation}`) must keep the MAUI build green — the `DailyReflection-maui.slnf` solution filter is the contract.
* **Acceptance criteria.** Specs list testable criteria, not aspirations. If a criterion needs a runtime check on a specific platform target, it says so.
* **Done when.** Each spec ends with a checklist that maps 1:1 to the criteria, suitable for use as a PR description.
* **Build verification.** Every spec assumes `dotnet build heads/DailyReflection.Uno/DailyReflection.Uno.sln` runs clean for the targets the spec touches before the spec is closed.

## What is *not* in scope

* Avalonia head — explicitly out of scope per the original analysis brief.
* Cross‑port feature parity beyond Xamarin → Uno. Where MAUI behaves differently from Xamarin (§4.7 of the analysis), specs follow the **Xamarin** behaviour, since that was the user's stated source‑of‑truth.
* Visual redesign. Per `prompt.md` the Uno port must not introduce a new design language. All UI work in these specs is to *restore* original behaviour, not to refresh it.
