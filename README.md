# Community Map

This project is a Blazor web application that lets people add their hometown to a shared community map and see where others in the Thrive St. George community are from. It combines a simple form for location submissions with a map view that groups entries by city and state.

## Project description

The app is designed to help a community connect by collecting and visualizing where members call home. Users can:

- add their city and state
- optionally include their name and email
- submit a location once per browser session
- view a community map of all submitted locations
- browse the aggregated list of cities and counts
- see shared community contact details for a selected city when available

The application stores submissions in PostgreSQL and uses geocoding to translate a city and state into latitude and longitude coordinates before plotting them on the map.

## What the app does

The experience is split into two main parts:

1. Submission flow
   - Visitors enter a city and state on the home page.
   - The app validates the input and geocodes the location.
   - The data is saved to the database.
   - A cookie prevents duplicate submissions from the same user.

2. Community map
   - The map page loads all saved submissions.
   - Pins are grouped by city/state and displayed as a shared map view.
   - The app shows totals for each place and offers a contact list for cities where people shared their details.

## Tech stack

- .NET 10 / ASP.NET Core
- Blazor Server
- Entity Framework Core with PostgreSQL
- OpenStreetMap Nominatim for geocoding
- Docker Compose support for local containerized running

## Project structure

- `CityMapApp/` - main Blazor application
- `Shared/` - shared DTOs and request/response models
- `CityMapApp.ServiceDefaults/` - common service configuration
- `docker-compose.yml` - Docker Compose setup
- `CityMapApp.sln` - solution file

## How to use it

### Run locally with the .NET CLI

From the project root:

```bash
dotnet build CityMapApp.sln
cd CityMapApp
dotnet watch run
```

Then open the local URL shown by the app in your browser.

The local .NET process expects PostgreSQL at `localhost:5432` with database
`citymap`, user `citymap`, and password `citymap`. The easiest way to provide it
is to run `docker compose up postgres` in another terminal.

### Run with Docker Compose

From the project root:

```bash
docker compose up --build
```

This builds and starts the application using the provided Docker configuration.

The Compose configuration starts PostgreSQL with a persistent named volume. In
production, set `ConnectionStrings__CityMapDb` to the managed PostgreSQL
connection string in the container environment; do not commit production
credentials. Database schema updates are applied through EF Core migrations at
startup.

## Typical workflow

1. Open the homepage.
2. Enter your city and state.
3. Optionally add your name and email.
4. Submit the form.
5. Visit the map page to see the community overview.
6. Click a city to view people who shared their contact information.

## Notes

- The application is meant for community engagement rather than large-scale production data processing.
- Geocoding depends on the OpenStreetMap Nominatim service, so internet access is required for location lookup.
- Duplicate protection is handled through a cookie-based user token for the same browser.

## License

This project is licensed under the terms included in the repository.
