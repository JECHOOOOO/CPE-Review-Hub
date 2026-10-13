# MySQL setup

## Local database

Use MySQL 8+ with TLS enabled. This project uses Oracle's
[`MySql.EntityFrameworkCore` 10.0.9](https://www.nuget.org/packages/MySql.EntityFrameworkCore/10.0.9) provider for EF Core 10, plus the ASP.NET Core
Identity EF store. It does not use SQLite, EF InMemory, or MariaDB.

The detected local installation is MySQL Server 8.0, Windows service `MySQL80`,
listening on port **3306**. XAMPP's separate database was detected on port **3307**.
Use port 3306 for this setup. XAMPP does not need to run. Do not start another
database server on the same port.

Open MySQL Workbench and connect using your existing operator account. Create a
new schema for this application:

```sql
CREATE DATABASE IF NOT EXISTS cpe_review_hub CHARACTER SET utf8mb4;
```

In **Server > Users and Privileges**, create a dedicated application user,
choose a strong password locally, and grant privileges on **cpe_review_hub only**.
For local setup/migrations, that schema grant needs to permit table creation,
indexes, schema alterations, and normal reads/writes. Do not grant server-wide
administration rights. Match the user's allowed host to the connection host
(normally localhost / 127.0.0.1).

Stop the application and run **scripts/Setup CPE Review Hub.cmd**. Default prompts:

| Setting | Suggested local value |
| --- | --- |
| Host | 127.0.0.1 |
| Port | 3306 |
| Database | cpe_review_hub |
| MySQL username | cpe_review_hub, or your dedicated account |
| MySQL password | Enter privately; there is no default |

Setup requires the database to exist, checks MySQL 8+ and TLS connectivity,
then saves `ConnectionStrings:ReviewHub` through `dotnet user-secrets set` over
stdin. It never puts the password in a command argument or source file.
It next applies the checked-in migration, and optionally provisions an admin.
It does not promote an existing student or reset an existing database.

User-secrets are per Windows user, outside Git, **not encrypted**, and suitable
only for local development. Do not copy their contents into appsettings or chat.

## Individual operator commands

Run from the project folder with the app stopped:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --no-launch-profile -- --configure-mysql
dotnet run --no-launch-profile -- --initialize-database
dotnet run --no-launch-profile -- --seed-course-catalog
dotnet run --no-launch-profile -- --create-admin
```

The catalog command idempotently installs or repairs the current 38-course
catalog (4 Freshman, 11 Sophomore, 16 Junior, and 7 Senior courses), their six
course-specific lessons, study guides, activities, published
10-item quizzes and published 50-item final exams. Questions, four choices,
correct-answer flags and feedback are derived from the same lesson blueprint so
assessments do not introduce untaught topics. The command is safe to rerun and
does not erase student data. The last command prompts for admin credentials.
These are website credentials, separate from the MySQL user. Existing emails
cannot be overwritten/promoted. Students register only through
`/Student/Account/Register`.

## Migrations

`src/CPE-Review-Hub/Data/Migrations` contains the Identity account schema, learning-content
foundation, activity/assessment/question-bank schema, and student activity and
assessment progress schema. Runtime startup does not automatically change the
database. Apply migrations deliberately with `--initialize-database`; it uses
EF migrations, not EnsureCreated or a reset. Back up existing application data
before applying future schema changes.

To generate a future migration:

```powershell
dotnet tool restore
dotnet ef migrations add DescriptiveChangeName --output-dir Data/Migrations
```

The local tool version is pinned in `dotnet-tools.json`. EF design-time tooling
does not require a working database to generate the initial migration.

## Access rules

- Student and Admin have different cookies, login actions, views and policies.
- Every protected request checks the user ID, account type, active flag,
  security stamp and lockout state against MySQL.
- Student registration hard-codes the account type on the server, regardless
  of extra submitted fields. Profile editing never accepts an account ID/role.
- Successful login clears both portal cookies, establishing one active portal
  for that browser. Sessions last at most two hours and are not persistent.
- Five incorrect password attempts lock the account for 15 minutes. Account
  POSTs are also limited to 20 per minute per remote IP; retry after a minute
  if the server returns 429. This limit may need tuning for campus NAT use.
- All MVC write requests require an antiforgery token. Logout uses POST.
- Disabling/re-enabling a student rotates their security stamp, invalidating
  old sessions. Public users cannot create administrators.
- A missing connection, missing schema, or unavailable server returns a safe
  503 page. The public home/login pages still render without MySQL.

## Production requirements

Use HTTPS, a stable protected ASP.NET Data Protection key store, and a proper
secret store or protected environment variable `ConnectionStrings__ReviewHub`.
Use MySQL TLS certificate verification (`SslMode=VerifyFull` plus the appropriate
CA configuration) for remote production servers. Local setup uses TLS Required.
Use a separate migration account; restrict the normal runtime account to needed
reads/writes, without DDL permissions. Do not run the setup UI as a public web
endpoint or expose MySQL publicly.

Before real student deployment, add institutional account approval/email
verification and password recovery, admin MFA as appropriate, database backups,
and the institution's privacy/retention policy. These are not implemented yet.
Configure trusted proxy handling before deployment behind a reverse proxy so
HTTPS and per-client rate limiting behave correctly.

## Troubleshooting

- **Port 5293 already in use:** an app is already running. Stop its console
  before rebuilding/setup, or use the existing app URL.
- **MySQL connection fails:** verify MySQL80 is running, the selected port is
  correct, the schema exists, TLS is enabled, and the database user has matching
  host permissions. Do not change/root-reset passwords to troubleshoot the app.
- **Database connection needed:** rerun setup or apply migrations. No fake
  in-memory data is substituted if MySQL is unavailable.
- **Student cannot use admin login:** expected. Create an administrator with
  the operator command; do not change browser storage or pass a role field.
