# FocusTrack

A Windows desktop productivity tracker built with C# / .NET 10 and Windows Forms for
CSCI 22042 — Visual Programming (University of Kelaniya).

FocusTrack watches which application window is in the foreground, logs each session to a
local SQLite database, classifies applications into categories with daily time goals, and
shows the results through a Dashboard, History Browser, and Reports view — with desktop
notifications when a goal is exceeded.

## Solution structure

```
src/
  FocusTrack.Data/       Entities, FocusTrackDbContext (EF Core Code-First), repositories, migrations
  FocusTrack.Business/   WindowTrackerService (P/Invoke), CategoryService, NotificationService, ReportService
  FocusTrack.UI/         WinForms shell, Dashboard/History/Settings/Reports forms, tray icon, DI wiring
```

Layering rule enforced throughout: **UI → Business → Data**. The UI never calls
`DbContext` or a repository directly — only `FocusTrack.Business` interfaces.

## Requirements

- Windows 10/11
- .NET 10 SDK
- Visual Studio 2022 (17.10+) or `dotnet` CLI

## Setup

```bash
git clone <repo-url>
cd FocusTrack/src
dotnet restore
```

### First-time database setup

The initial migration needs to be created once and is checked into `FocusTrack.Data/Migrations`:

```bash
cd FocusTrack.Data
dotnet ef migrations add InitialCreate --startup-project ../FocusTrack.UI
```

The app applies pending migrations automatically at startup (`db.Database.Migrate()` in
`Program.cs`), so after the first migration is committed, teammates just need to `dotnet restore`
and run — no manual `dotnet ef database update` step required.

The SQLite file is created under:
`%LOCALAPPDATA%\FocusTrack\focustrack.db`

## Run

```bash
cd FocusTrack.UI
dotnet run
```

Or open `FocusTrack.sln` in Visual Studio and press F5 (set `FocusTrack.UI` as the startup project).

FocusTrack starts tracking immediately and minimises to the system tray when you close the
main window — use the tray icon's context menu to reopen it or exit fully.

## Team / Contributions

See the Requirements Document (Section 3, Team Task Breakdown) for the per-member branch and
feature ownership, and the final project report for individual contribution summaries.

## AI assistance disclosure

Initial architecture, entity/service scaffolding, and the UML diagrams in the Requirements
Document were produced with AI assistance (Claude, Anthropic) as a design and learning aid,
per the module's academic integrity policy (CQA/A/P/03). All feature implementation, testing,
debugging, and integration were completed and committed individually by team members.
