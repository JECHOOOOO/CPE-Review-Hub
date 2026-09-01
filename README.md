# CPE Review Hub

Existing ASP.NET Core MVC application targeting .NET 10.

## Configure MySQL first (one time)

Use **MySQL 8 or newer**, not SQLite or XAMPP's MariaDB. The development PC has
the `MySQL80` Windows service. It must be running when using accounts.

1. Create a `cpe_review_hub` database and a dedicated MySQL user with access to
   that database. See [MySQL setup](docs/MYSQL-SETUP.md).
2. Stop the app if it is running, then double-click **Setup CPE Review Hub.cmd**.
3. Enter your MySQL connection details in its local prompts. Password input is
   hidden. The connection is tested before it is saved in .NET user-secrets.
4. Setup applies versioned migrations and installs the exact 34-course catalog
   without resetting users or existing attempts. Choose **Y** to create your
   administrator, then enter the admin email, name and password.
5. Use the Start launcher below. Students register at `/Student/Account/Register`.

MySQL credentials and website accounts are separate. There is no default admin
password and no public admin registration. Never send passwords in chat or
commit them to Git. User-secrets are unencrypted local development storage,
outside this repository; use a protected secret store for deployment.

## Start on Windows

1. Install the .NET 10 SDK if it is not already installed.
2. Double-click **Start CPE Review Hub.cmd** in this folder.
3. Wait for the browser to open at **http://localhost:5293**.

Keep the server window open while using the site. Closing the browser does not
stop the server. Press **Ctrl+C** in the server window to stop it. Start the
launcher again after stopping the server or restarting your PC.

If the port is already in use, the launcher will not start another server. If
the existing server is CPE Review Hub, open the URL above yourself; otherwise,
stop the conflicting application first. Build/startup errors appear in the
console, which stays open so they can be read. An initial build may require
internet access for package restore.

For a desktop shortcut, right-click the launcher and choose **Show more options
> Send to > Desktop (create shortcut)**. Keep the launcher and `scripts` folder
together in the repository; create a shortcut instead of moving the launcher.

The launcher uses Windows PowerShell with an execution-policy override for
that process only; it does not change the machine's execution-policy settings.

## Current scope

The MVC app has separate `/student.html` and `/admin.html` login pages,
MySQL-backed Identity accounts, student registration, protected dashboards,
student profile editing, and admin student search/enable/disable. Authentication
uses separate protected cookies, layouts, routes and server-verified account
types. Registering through the student portal can never grant admin access.

The catalog contains the requested 34 courses: 11 at 200-Level, 16 at
300-Level, and 7 at 400-Level. Every course has six published lessons. Every
lesson includes a substantial, course-specific editable study guide with
objectives, explanations, definitions, procedures, examples and takeaways. A
required activity appears before its published 10-item quiz. Every course also
has a published 50-item final exam assembled only from concepts taught in its
six lessons. Course cards and lessons are clickable.

Administrators can create, edit, publish and delete subjects, lessons and plain-
text review materials. Students can browse published content by the fixed 200,
300 and 400 academic levels. Draft content is never returned by Student routes.
The database stores 2,040 lesson questions with four choices and feedback,
reusable assessment-question ordering, activity submissions, assessment
attempts and selected answers. A student must submit the lesson activity before
its quiz unlocks. The final exam unlocks after all six activities and lesson
quizzes are completed. Quiz and exam scores, answer review, history and course
progress are available from the Student portal. Admin question/choice editing
screens remain a future management feature; the normalized database structure
already supports them. No demo accounts, scores or hard-coded passwords are
seeded.

## Start manually

From this repository folder:

```powershell
dotnet run --launch-profile http
```

Then open http://localhost:5293 in your browser.

## Verify

```powershell
dotnet build
python tests/verify_mysql.py
```

The integration test requires Python 3 and installed MySQL 8 Windows binaries.
It starts its own loopback-only MySQL server on an unused port, creates random
test credentials, and removes only its own temporary directory under `obj`.
It does not connect to your MySQL80 service or use your database credentials.
It checks migrations, persistence, passwords, CSRF, account types, portal
isolation, profile ownership, account disabling, content CRUD, draft filtering,
the exact grouped 34-course catalog, six-lesson structure, long study guides,
activities, question and choice counts, activity gating, 10-item quiz scoring,
answer feedback, progress, Student content browsing, lockouts, logout and outages.
