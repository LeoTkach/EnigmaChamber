# EnigmaChamber

Admin panel for escape room bookings, game runs, and statistics — styled as a retro CCTV security console.

**Stack:** ASP.NET Core 8 · Razor Pages · EF Core 8 (Code-First) · SQL Server 2022 · Serilog · Docker

## Quick start

```bash
cp .env.example .env
docker compose up -d --build
```

Open **http://localhost:8080** — the app applies EF migrations (including all T-SQL objects) and seeds demo data automatically on startup.

## Features

| Page | What it shows |
|---|---|
| Home | Today's games with the operator workflow: start → complete → enter result, cancel |
| Rooms | CRUD for rooms (difficulty, players range, price, age limit, actor requirement) |
| Bookings | Weekly slot schedule per room (`fn_FreeSlots`) + journal with staff assignment; creation goes through `sp_CreateBooking` |
| Staff | CRUD for game masters and actors |
| Results | Result entry after a game + Hall of Fame (best final times per room) |
| Stats | Monthly per-room report computed by the `sp_MonthlyStats` CURSOR procedure |
| Audit | Read-only log filled by the `trg_Bookings_Audit` trigger |

## T-SQL objects (lab part 1)

Created by EF migrations; standalone reference scripts live in `sql/`.

- **Stored procedures:** `sp_CreateBooking` (slot validation + insert), `sp_MonthlyStats` (CURSOR over rooms)
- **Scalar function:** `fn_FinalTime` (elapsed + 2 min penalty per hint)
- **Table-valued function:** `fn_FreeSlots` (slot grid 10:00–22:00 with a 15-min buffer)
- **Triggers:** `trg_Bookings_NoOverlap`, `trg_Bookings_Audit`, `trg_RunResults_SetFinalTime`

## Web app (lab part 2)

- EF Core Code-First with 3 migrations, CRUD, validation (data annotations + server-side rules)
- Dependency Injection (services: rooms, bookings, results)
- Logging with Serilog (console + rolling files in `logs/`)
- In-memory caching of the room list (`IMemoryCache`)

## Project structure

```
sql/           T-SQL reference scripts (schema, procedures, functions, triggers, cursors)
src/           EnigmaChamber.Web — Razor Pages application
scripts/       helper scripts (manual SQL apply)
docs/          ER diagram, report assets
```

## Course

KhNURE — «Програмне забезпечення .NET» · lab practicum
