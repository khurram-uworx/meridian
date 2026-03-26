# 📜 Meridian

> **Source-native. Jira-native. Out of your way.**

An engineer-first Learning Management System where courses live in Git or local folders and student learning journeys are instantiated entirely in Jira — as Epics, Stories, and Comments. Meridian is the thin layer that bridges the two.

---

## The Idea

Every other LMS is a third system. Engineers already live in Git and Jira. Meridian eliminates the context switch entirely.

- **Courses** are source-controlled or local folders — Git repo roots, subfolders in a shared repo, or absolute local paths
- **Enrollment** scaffolds a Jira Epic + Stories directly from the selected course folder
- **Progress** is just Jira — same board, same workflow, same process your team already uses
- **Quizzes and progress views** are the only UI Meridian provides — because Jira can't render MCQs

---

## How It Works

```
course folder source (Git or local path)
    course.yaml          ← course metadata, ordering
    00-intro.md          ← becomes a Jira Story
    01-setup.md          ← becomes a Jira Story
    02-ci-cd.md          ← becomes a Jira Story (with quiz link if defined)

         │
         │  enrollment trigger
         ▼

Jira Epic  (Ben → X102)
    ├── TICKET-101  Introduction          [To Do]
    ├── TICKET-102  Setup & Prerequisites [In Progress]
    └── TICKET-103  CI/CD Basics          [Done — quiz score: 9/10 in comments]
```

Ben never touches the source folder. The curriculum lives in files. Jira is Ben's experience.

---

## MD File Structure

Each Markdown file in a course folder uses YAML frontmatter to tell Meridian how to scaffold the Jira ticket:

```markdown
---
title: "Introduction to CI/CD"
order: 3
type: lesson          # lesson | quiz | lab
story_points: 3
quiz: intro-cicd-q1   # omit if no quiz
depends_on: 02-prerequisites
---

## What is CI/CD?

Content here becomes the Jira ticket description...
```

Ordering rule for PoC: `order` in frontmatter is the source of truth. If omitted, Meridian falls back to filename prefix ordering (`00-`, `01-`, ...).

---

## Course Config (`course.yaml`)

```yaml
id: X102
title: "CI/CD for Backend Engineers"
version: 1.0.0
author: platform-team
jira_project: LEARN
epic_label: "meridian"
```

---

## Tech Stack

| Layer | Choice | Reason |
|---|---|---|
| Backend | ASP.NET Core (.NET 10) | Pragmatic, strong ecosystem |
| ORM | Entity Framework Core | In-Memory for PoC, Postgres/SQL Server later |
| Git integration | LibGit2Sharp | Clone, read, SHA tracking |
| Jira integration | Dapplo.Jira | Epic + Story creation, polling, see src\Meridian.Tests\DapploJiraTests.cs for usage patterns |
| UI | ASP.NET MVC + Razor | No JS framework overhead |
| Auth | Jira API token (PoC), OAuth later | Keep PoC setup simple while reusing Jira identity model |

---

## PoC Scope

The first working version does exactly this and nothing more:

- [x] `course.yaml` + MD files define a course in one of:
  - Git repo root
  - Git repo subfolder (for example, `courses/course1`, `courses/course2`)
  - Absolute local folder path (for example, `C:\courses\course1`)
- [x] `POST /enroll` — takes learner + course source locator, resolves the course folder, records source revision when applicable, creates Jira Epic + Stories
- [x] Jira Stories get MD content as description + frontmatter mapped to fields
- [x] Quiz sections generate a Meridian-hosted link embedded in the ticket
- [x] Meridian quiz UI — MCQ, submit, write score back as Jira comment, transition ticket
- [ ] Learner progress view — Meridian polls Jira for its known Epics and renders completion state
- [ ] Learner history — "Ben completed X101 on Jan 3, X102 enrolled Mar 25"

**Out of PoC scope:** live sync from repo changes (post-PoC: diff from enrollment SHA)

---

## Data Model (PoC)

```
Learner        (Id, Name, Email, JiraAccountId)
Course         (Id, SourceType, SourceLocator, CoursePath, CourseYamlSnapshot)
Enrollment     (Id, LearnerId, CourseId, JiraEpicKey, EnrolledAt, SourceRevision)
QuizAttempt    (Id, EnrollmentId, QuizId, Score, MaxScore, AttemptedAt, JiraCommentId)
```

EF Core In-Memory for PoC. Migration to Postgres or SQL Server requires zero model changes.

---

## Plan of Action

### Phase 0 — Repo & Skeleton
- [x] GitHub repo is initialized: `meridian`
- [x] `.NET 10` solution exists: `Meridian.slnx`
  - `Meridian` — ASP.NET MVC project
  - `Uworx.Meridian` — domain models, interfaces
  - `Uworx.Meridian.Infrastructure` — EF, Git, Jira integrations
- [x] EF In-Memory wired up, migrations ready to swap provider
- [x] `appsettings.json` stubs for Jira base URL, API token, default project key

### Phase 1 — Course Parsing
- [x] Resolve course source:
  - Git URL (clone via LibGit2Sharp)
  - Local absolute path (read directly from disk)
  - Existing local repo + course subfolder path
- [x] Parse `course.yaml` into `CourseConfig`
- [x] Parse MD frontmatter (YAML) from each `.md` file into `SectionDefinition`
- [x] Unit tests: given source type + course path, assert correct sections are parsed in order
- [x] Integrated `ICourseParser` service

### Phase 2 — Enrollment & Jira Scaffolding
- [x] Jira service: `CreateEpic()`, `CreateStory(epicKey, title, description, storyPoints, label)`
- [x] Enrollment flow: resolve source → parse → create Epic → loop sections → create Stories
- [x] Persist `Enrollment` record with `SourceRevision` (Git SHA or `null` for local folder) and `JiraEpicKey`
- [x] Simple Razor page: enroll form (learner email + source locator + optional course subpath) → confirm page showing Epic link

### Phase 3 — Quiz Flow
- [x] Quiz definition in MD frontmatter (question/options in YAML or linked JSON)
- [x] Meridian quiz UI: `/quiz/{quizId}?enrollment={id}` — render MCQs, submit
- [x] On submit: calculate score, POST comment to Jira ticket, transition ticket to Done
- [x] Persist `QuizAttempt`

### Phase 4 — Progress & History View
- [ ] `/learner/{id}` — poll Jira for all known Epics, render ticket states as progress
- [ ] Course completion % from Done tickets / total tickets
- [ ] Enrollment history timeline across courses

---

## Running Locally (PoC)

```bash
# Prerequisites: .NET 10 SDK, Jira API token

git clone https://github.com/khurram-uworx/meridian
cd meridian

# Create local dev config from template
cp src/Meridian/appsettings.Development.template.json src/Meridian/appsettings.Development.json
# PowerShell equivalent:
Copy-Item src/Meridian/appsettings.Development.template.json src/Meridian/appsettings.Development.json
# edit: Jira.BaseUrl, Jira.ApiToken, Jira.UserEmail, Jira.ProjectKey

dotnet run --project src/Meridian
# → http://localhost:5000
```

---

## Contributing & Course Authoring

A course is just a folder with `course.yaml` and Markdown files. To author one:

1. Choose a source pattern:
   - Dedicated Git repo (course at repo root)
   - Shared Git repo with multiple courses (for example, `courses/course1`, `courses/course2`)
   - Absolute local folder (for example, `C:\courses\course1`)
2. Add `course.yaml` at the course root
3. Add numbered Markdown files at the course root — one per section
4. Use YAML frontmatter for Meridian metadata
5. Point Meridian at the source locator (+ optional subpath) to enroll a learner

Engineers and their copilots will feel right at home.

---

## License

MIT
