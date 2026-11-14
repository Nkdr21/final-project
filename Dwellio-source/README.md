# Dwellio

A student housing web application for Astana. Find a home by rent, everyday amenities and the journey to campus and back. Built with C#/.NET 10, Blazor Interactive Server, EF Core, SQLite and ASP.NET Core Identity.

**Data status:** seeded homes and complexes are fictional; images are original SVG concept illustrations. Transport and walking times are explicit demo scenarios, not real-time forecasts. The OpenStreetMap base map is external. No API key is needed to run.

## Run in VS Code

Install .NET SDK 10 and the recommended C# Dev Kit extension. Open this repository folder, then run:

```sh
dotnet restore
dotnet run --project src/Dwellio.Web --launch-profile http
```

Open `http://localhost:5180`. Stop with Ctrl+C. VS Code also has **Terminal → Run Task → Dwellio: run**.

SQLite files are created automatically under `src/Dwellio.Web/App_Data/`. They are ignored by Git. Identity uses migrations; the classroom housing schema uses `EnsureCreated`. Future schema changes need a migration plan, not deletion of user data.

## Accounts

Register through **Sign in → Register as a new user**. Check **I want to publish listings** for an Owner account. Otherwise the account is a Student. Users cannot self-register as Admin. Owner access only permits editing their own listings. Student preferences and saved homes are private to their account.

For local classroom demonstration, optionally set `DWELLIO_DEMO_PASSWORD` to a password you choose that satisfies Identity rules before the first launch with this setting. It creates `student@dwellio.test`, `owner@dwellio.test`, and `admin@dwellio.test`. Never put this password in source control or shared scripts. The app does not reset existing passwords when the variable changes. No credentials are shipped.

Email delivery is not configured; email confirmation is disabled for this local classroom version. Real deployment needs an email provider, confirmed accounts, HTTPS, protected key storage, upload lifecycle controls and deployment testing.

## Main workflow

1. Open **My lifestyle** and set your budget, must-haves, desired amenities and commute times.
2. Select **Show my matches**. Use filters and sorting; add up to three homes to comparison.
3. Open a home to see explanations, costs, outbound/return scenarios and its neighbourhood.
4. Sign in to save homes and preferences or review a complex.
5. Owners publish/edit listings in Dashboard; Admin can hide/restore listings and reviews.

Comparison selection is scoped to the current interactive session. Profile preferences and favourites are stored in SQLite. A full reload starts a new comparison session.

## Project structure

```text
src/Dwellio.Core/       Immutable models and pure functions (no database or UI)
src/Dwellio.Web/        Blazor UI, Identity, SQLite, services and illustrations
tests/Dwellio.Tests/    Unit and real SQLite integration tests
docs/                  Architecture, course mapping, team plan and handoff
scripts/               Local verification helpers
```

## Verification

```sh
dotnet build
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

With the app running, `python3 scripts/http_smoke.py` checks public HTTP routes and access redirects. This is not a browser end-to-end test. See `docs/testing.md` for the actual verification status and remaining manual tests.

GitHub Actions is configured for Windows, Linux and macOS; this matrix is not verified until the workflow runs after you push.

## Team and commits

See `docs/team-plan.md` and `docs/friend-branch-ru.md`. The baseline was prepared with AI assistance; commits should describe actual review and changes, not invented authorship. The repository owner requested to perform the next commit personally. The only existing remote commit at handoff is the initial `.gitignore` commit unless the owner has pushed independently.

## Important documents

- `docs/grading-checklist.md`: honest evidence for all 20 criteria
- `docs/lecture-mapping.md`: functional C# concepts and their implementation
- `docs/HANDOFF.md`: instructions for continuing in another chat
- `docs/data-sources.md`: data provenance and limitations

This is a working classroom application, not a verified rental marketplace or a guarantee of a particular grade.
