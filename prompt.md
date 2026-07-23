You are an expert in .NET MAUI, .NET, Uno Platform, WinUI, and cross-platform migrations. Migrate an existing MAUI app to Uno Platform by creating a new Uno solution (keep the original MAUI project untouched). Use the MCP exactly as follows: Uno Platform Docs MCP must be queried before every decision.

## Strict constraints

- No visual redesign or layout changes. Do not introduce Material, AutoLayout, color systems, or new styles. Preserve existing UI structure, bindings, converters, resources, and behavior.
- No aggressive refactors. Keep the original navigation shape and DI unless a doc-confirmed mapping requires adjustment.
- Do not remove features; if a gap exists, ask Docs MCP for guidance first.
- You MUST use your available tools to complete the task
- You MUST list your available tools before starting the work
- You MUST extensively use uno_platform_docs_search and uno_platform_docs_fetch for best guidance on usage
- You MUST use all tools available from the UnoApp MCP Server
- You MUST initialize the agent rules with uno_platform_agent_rules_init and uno_platform_usage_rules_init
Process:
- Inventory: scan the repository and list all MAUI Pages, ViewModels, Models, Services, Resource Dictionaries, Converters, Behaviors, Effects, and any Shell/NavigationPage/TabbedPage usage.
- Plan: for each area (views, navigation, DI, platform services, resources), propose a migration plan. For every uncertain mapping, first ask Uno Platform Docs MCP, then proceed.
- Create Uno app: generate a new Uno solution.
- Views: migrate each MAUI XAML view to Uno XAML, preserving layout and bindings. Keep existing names and structure where possible. Do not introduce new styles or controls. Only substitute controls when Docs MCP confirms a required mapping.
- Navigation: migrate MAUI navigation to the closest Uno pattern without redesign. Examples (subject to Docs MCP confirmation): Shell/NavigationPage/TabbedPage to NavigationView + Frame; PushAsync/PopAsync to Frame.Navigate/GoBack. Confirm each mapping with Docs MCP before changing.
- You MUST use the same permissions in the AndroidManifest.xml as in the original MAUI project.
- Reuse existing ViewModels, Commands, and Models if possible. Keep namespaces consistent unless a change is required.
- Essentials/plugins: for each API (Preferences, Permissions, Geolocation, File access, Connectivity, Media, etc.), ask Docs MCP how to map it in Uno.
- Resources: migrate Resource Dictionaries, styles, and converters as-is. Do not invent keys or change color/typography systems. If something must change to compile/run, confirm with Docs MCP and document the minimal fix.
- Iteration loop (repeat per subsystem/file): show original file → show migrated file → cite the exact Docs MCP response used → build the Uno app with Uno App MCP → run on desktop target → verify navigation, bindings, and feature parity. Fix compile/runtime issues before proceeding.
- Completion: ensure the Uno app builds cleanly, runs, and matches the original behavior and structure. Provide a final checklist covering navigation flows, bindings, platform services, and resource usage, plus a list of any gaps with links to the Docs MCP answers.
Execution rules:
- Always query Uno Platform Docs MCP before replacing any MAUI API/control/navigation/binding. Include the citation of the doc answer in your explanation for each change.
- Make changes in small atomic commits; after each major step, compile and run.
- Do not delete or modify the original MAUI project.