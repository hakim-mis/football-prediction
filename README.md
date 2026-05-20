# Football Prediction Game System - Enhanced Edition

Professional ASP.NET Core MVC football prediction system with SQL Server database, Identity authentication, admin approval, fixture segments, prediction scoring, password reset workflow, graphs and a responsive football-style dashboard.

## Technology

- ASP.NET Core MVC .NET 8
- Microsoft SQL Server
- Entity Framework Core
- ASP.NET Core Identity
- Bootstrap 5
- Chart.js

## Main Features

### User

- Register with photo, email, mobile number and password
- Account remains pending until admin approval
- Login after activation
- Dashboard showing:
  - Total score
  - Current rank
  - Exact match count
  - Open fixtures
  - Top 10 leaderboard
  - Segment-wise points graph
  - Today fixtures
  - Recent predictions
- Predict match score before match start
- View points by fixture segment such as Group A, Group B, Semi Final and Final

### Admin

- Dashboard with card layout similar to modern analytics panels
- Summary cards and graphs
- Activate/deactivate users
- Reset user password to the default password
- User must set a new password immediately after logging in with the default password
- Create, edit, delete and publish fixtures
- Upload team flags
- Assign fixture segment:
  - Group A
  - Group B
  - Group C
  - Group D
  - Quarter Final
  - Semi Final
  - Final
- Enter actual scores
- Process match result once only
- View leaderboard overview

## Default Admin Login

```text
Email: admin@football.local
Password: Admin@12345
```

Change the admin password after first login.

## Default User Reset Password

When admin resets a user password, the default password is:

```text
User@12345
```

After login with this password, the user is forced to set a new password before accessing the dashboard.

You can change this value in `appsettings.json`:

```json
"Security": {
  "DefaultResetPassword": "User@12345"
}
```

## Database Setup

1. Open SQL Server Management Studio.
2. Run this file:

```text
Database/01_CreateDatabase.sql
```

The script creates:

- FootballPredictionDb
- ASP.NET Identity tables
- Fixtures
- Predictions
- ResultProcessingLogs
- Required indexes
- Upgrade columns for enhanced version

## Connection String

Default connection string:

```json
"DefaultConnection": "Server=.;Database=FootballPredictionDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

For SQL Server Express:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=FootballPredictionDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

## Run Application

From the project folder:

```bash
dotnet restore
dotnet run
```

Or open the solution in Visual Studio and press F5.

## Scoring Rules

| Prediction Type | Points |
|---|---:|
| Exact score | 3 |
| Correct result/winner/draw only | 1 |
| Wrong result | 0 |

Example:

Actual result: Argentina 3 - 2 Brazil

| User Prediction | Points |
|---|---:|
| Argentina 3 - 2 Brazil | 3 |
| Argentina 2 - 1 Brazil | 1 |
| Brazil 2 - 1 Argentina | 0 |

## Important Security Rules Included

- ASP.NET Core Identity password hashing
- Role-based authorization
- Admin approval before user login
- Secure file upload validation
- One prediction per fixture per user
- No prediction allowed after match start
- Result processing cannot run twice for the same fixture
- Forced password change after admin reset
- Anti-forgery validation on POST actions

## UI Changes in Enhanced Edition

- Light blue page background
- White analytics cards similar to the provided reference image
- Off-white tables with thin blue borders
- Segment badges for group/round display
- Chart.js graphs for segment points and fixture status
- Cleaner admin and user dashboard layout

## Latest Update: Undo Processing and Shared Ranking

### Undo processed match result

If an admin enters the wrong actual score and already processes the fixture:

1. Go to Admin > Fixtures.
2. Click `Undo` beside the processed fixture.
3. The system subtracts the points earned from that fixture from each user.
4. Exact prediction counts are also reverted.
5. The fixture becomes unprocessed again.
6. Admin can edit the actual score and process the fixture again.

### Ranking rule

Ranking now uses shared rank by total score:

- If 3 users have 50 points, all 3 users show `#1`.
- The next lower score shows competition rank, for example `#4`.
- Users with 0 points show `No rank`.
- Exact prediction count and joined date are still used only for sorting users inside the same score group.

## Latest Updates in This Package

### Dense Ranking Rule
Ranking now uses dense ranking and ignores zero scores:

- 50 points -> Rank #1
- 50 points -> Rank #1
- 50 points -> Rank #1
- 40 points -> Rank #2
- 0 points -> No rank

### Prediction Lock Rule
Prediction saving now checks the live database fixture record before saving. A user can submit or update a prediction only when:

- Fixture status is `Upcoming`
- Fixture is not processed
- Current time is before the match start time

`Live` and `Finished` fixtures are locked. The dashboard also disables the prediction form for locked fixtures.

### Smart Fixture Card UI
The user dashboard fixture cards are now smaller and smarter:

- Upcoming fixtures have a blue status badge
- Live fixtures have a blinking red status badge
- Finished fixtures have a gray status badge
- Locked fixtures show the user's submitted prediction, if available
