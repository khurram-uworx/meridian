# AGENTS.md

## Project overview
- Meridian is an ASP.NET Core MVC app (`src/Meridian`) with a shared domain library (`src/Uworx.Meridian`), infrastructure (`src/Uworx.Meridian.Infrastructure`), and NUnit test project (`src/Meridian.Tests`).
- The app provisions Jira Epics/Stories from source-authored courses (`course.yaml` + markdown files).
- Primary docs: `README.md`, sample course content in `Courses/`.

## Dev environment tips
- Required SDK: .NET 10 (`net10.0` across all projects).
- Restore/build/test from repo root:
  - `dotnet restore`
  - `dotnet build --configuration Release`
  - `dotnet test --configuration Release`
- Run web app:
  - `dotnet run --project src/Meridian`
- Local Jira settings:
  - Copy `src/Meridian/appsettings.Development.template.json` to `src/Meridian/appsettings.Development.json` and fill Jira values.

## Repository map
- `src/Meridian`: ASP.NET Core host, controllers, Razor views, SignalR hub, background enrollment processing.
- `src/Uworx.Meridian`: interfaces, entities, config models, shared contracts.
- `src/Uworx.Meridian.Infrastructure`: Jira integration, EF Core DbContext, course parsing/resolution logic.
- `src/Meridian.Tests`: unit + integration tests (NUnit).
- `.github/workflows/ci.yml`: canonical CI pipeline.

## Testing instructions
- CI sequence is: restore -> build -> test (`.github/workflows/ci.yml`).
- Integration tests exist and may touch Jira APIs.
- Integration tests depend on env vars such as:
  - `JIRA_URL`
  - `JIRA_USER`
  - `JIRA_TOKEN`
  - `JIRA_PROJECT`
- If integration env vars are missing, integration tests are skipped with `Assert.Ignore`.
- Prefer running unit-focused checks while iterating:
  - `dotnet test --filter "Category!=Integration"`

## Coding conventions
- Follow `.editorconfig` (CRLF, UTF-8, final newline, 4-space indent for C#, 2-space for JSON/YAML).
- C# naming conventions are enforced as suggestions:
  - Private fields: `camelCase` without `_` prefix.
  - Public/protected/internal members: `PascalCase`.
  - Locals/parameters: `camelCase`.
- C# class should have
	- inner classes first, then constructors, then properties, then methods.
	- static members before instance members.
	- private members first, protected members second, internal members third, public members last.
- Its fine to keep multiple classes in the same file if they are small and closely related.
- Prefer record types for simple data carriers (e.g. config models, DTOs) and classes for entities/services.
- Keep nullable reference types enabled and avoid introducing warnings.
- Do not commit secrets/tokens in `appsettings.*.json` or test code.

## PR instructions
- Before opening a PR, run:
  - `dotnet restore`
  - `dotnet build --configuration Release`
  - `dotnet test --configuration Release`
- Keep changes scoped to the requested task; avoid unrelated refactors.
- When changing behavior, update/add tests in `src/Meridian.Tests`.
- If changing Jira integration behavior, document required env/config updates in `README.md` or PR notes.
