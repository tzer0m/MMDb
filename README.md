# MMDb

[![Deploy](https://github.com/tzer0m/MMDb/actions/workflows/deploy.yml/badge.svg)](https://github.com/tzer0m/MMDb/actions/workflows/deploy.yml)

My Movie Database: a self-hosted site for keeping my film ratings, built with ASP.NET Core Razor Pages on .NET 10.

## Features

- **Home**: a searchable, sortable table of every rated film with posters, my rating (MR) and the community rating (CR), plus bar charts of both rating distributions. Searching for a film that isn't in the list offers to add it.
- **Film pages**: poster, runtime, watched date, overview, my rating and the community rating, individual IMDb, TMDb, Rotten Tomatoes and Metacritic ratings linking to each site, and the director and top five cast with the other films of theirs I've rated.
- **Person pages**: photo, dates, birthplace and a short biography, average ratings, and every film of theirs I've rated.
- **Add and edit**: search TMDb, pick a film, rate it and set the watched date. Ratings are pushed to the film's Critics Rating in Jellyfin.
- **Community rating (CR)**: the average of the TMDb, IMDb, Rotten Tomatoes and Metacritic ratings on a 10-point scale, using whichever are available.
- **Background refresh**: a daily job refreshes each film's IMDb, Rotten Tomatoes and Metacritic ratings from OMDb once they're over 7 days old.

## Stack

- ASP.NET Core Razor Pages, .NET 10
- PostgreSQL with EF Core (Npgsql)
- Bootstrap 5 (dark theme) and DataTables
- [TMDb](https://www.themoviedb.org/) for film and person details, [OMDb](https://www.omdbapi.com/) for IMDb, Rotten Tomatoes and Metacritic ratings, and Jellyfin for pushing ratings

## Setup

1. Create a PostgreSQL database and a role for the app:

   ```sql
   CREATE ROLE mmdb_app WITH LOGIN PASSWORD 'change-me';
   CREATE DATABASE "MMDb";
   REVOKE CONNECT ON DATABASE "MMDb" FROM PUBLIC;
   GRANT CONNECT ON DATABASE "MMDb" TO mmdb_app;
   -- then, connected to MMDb:
   GRANT USAGE, CREATE ON SCHEMA public TO mmdb_app;
   ```

2. Add the secrets (Visual Studio: right-click the project, then Manage User Secrets):

   ```json
   {
     "ConnectionStrings": {
       "MMDb": "Host=...;Database=MMDb;Username=mmdb_app;Password=..."
     },
     "TMDb": {
       "ApiReadAccessToken": ""
     },
     "OMDb": {
       "ApiKey": ""
     },
     "Jellyfin": {
       "ApiKey": "",
       "UserId": ""
     },
     "Oidc": {
       "ClientId": "",
       "ClientSecret": ""
     }
   }
   ```

3. Run the app. Pending migrations are applied on startup.

## Configuration

Non-secret settings live in `appsettings.json`:

| Section | Purpose |
|---|---|
| `Oidc` | Pocket ID authority URL for signing in |
| `TMDb` | API and image base URLs |
| `OMDb` | API base URL, and how long cached OMDb ratings last for lookups, previews and library films |
| `Search` | How many TMDb results the lookup page shows |
| `RatingsRefresh` | How often the nightly job runs (ratings refresh, Jellyfin rating sync and library sync), how old ratings can get, and how many films to refresh per run |
| `People` | How long cached person details and TMDb credits last, how many cast members to store and show per film, and how many top rated films (with a minimum vote count) to show under Other Roles |
| `ExternalLinks` | URL templates for TMDb, IMDb, Rotten Tomatoes, Metacritic, GitHub and SwagBagger links |
| `Jellyfin` | Server URL, and how long the library list and collections are cached |

## Credits

- Logo and favicon: <a href="https://www.flaticon.com/free-icons/achievement" title="achievement icons">Achievement icons created by Uniconlabs - Flaticon</a>
- Film and person data from [TMDb](https://www.themoviedb.org/). This product uses the TMDb API but is not endorsed or certified by TMDb.
- Ratings from [OMDb](https://www.omdbapi.com/).
