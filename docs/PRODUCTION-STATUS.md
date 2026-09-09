# COLMS production deployment

## Architecture

Browser → HTTPS Vercel domain → .NET 10 MVC container → Aiven MySQL 8.4.
The personal computer is not part of the hosted runtime. Local development continues to use Windows user-secrets and the local database.

Vercel project: `jechteam/colms-reviewhub` (the same existing project, renamed from `colms_reviewhub` because its container registry rejected the underscore).
Assigned domain: `https://colmsreviewhub.vercel.app`. A successful build alone is not evidence that every application feature works.

`vercel.json` explicitly selects `Dockerfile.vercel`; automatic detection previously produced a 404 with no application build. Vercel builds and runs the original MVC application; there is no React/Next.js replacement. PORT is 8080. The project remains on its existing free Hobby plan; no paid upgrade was requested. Free-tier limits and service availability still apply.

## Database and persistence

The local database was backed up privately and copied into the initially empty Aiven `defaultdb`. All 19 original table row counts matched, including 8 accounts, 34 subjects, 204 lessons, and 2,040 questions. Local data was retained. Table names were normalized to the model's exact casing on Linux without deleting rows.

Migration `20260908182217_PersistProductionAssets` adds `DataProtectionKeys` and nullable `LessonImages.ImageData` (longblob) / `ContentType`. It was applied to both local and cloud databases. Existing image paths and content remain intact. No local uploaded image files existed at migration time.

Uploaded lesson images now use database bytes and `/images/lessons/stored/{id}`; bundled educational SVG diagrams remain under `wwwroot/images/lessons`. Uploaded images require authentication; unpublished images are administrator-only. File signatures are checked. Activity attachments already use database storage. Never use the container filesystem for new persistent uploads.

Data Protection keys are stored in MySQL and encrypted with an RSA certificate kept in production secrets. Preserve that certificate when redeploying; replacing it without retaining old decryption certificates invalidates existing cookies/tokens. Plan certificate rotation before its five-year expiry.

The dedicated `colms_runtime` user requires TLS and has SELECT/INSERT/UPDATE/DELETE only. Schema changes require a separate operator credential and deliberate migration after backup. Startup does not drop, reseed, or automatically migrate the database.

The Oracle MySQL driver uses `SslMode=VerifyCA` and the bundled public Aiven CA. Its PEM VerifyFull mode also requires a client certificate/key; those were not supplied. CA validation was verified successfully; do not describe this setting as hostname verification. No TLS validation callback was disabled.

## Routes and security

`/student/login`, `/student/register`, `/student/dashboard`, `/student/courses`.
`/admin/login`, `/admin/dashboard`, `/admin/courses`.
Original `/student.html`, `/admin.html`, and existing course/lesson routes remain compatible.

Student and Admin use separate cookie schemes and backend role policies. Public registration cannot grant Admin rights. Administrator activities remain read-only. Scores/progression remain automatic and database-backed. Production cookies are Secure/HttpOnly; antiforgery validation remains enabled. No arbitrary forwarded-header trust was added.

## Environment variable names

- `ConnectionStrings__ReviewHub`
- `DataProtection__PersistToDatabase`
- `DataProtection__CertificateBase64`
- `Application__PublicBaseUrl`
- `ASPNETCORE_ENVIRONMENT`
- `PORT`
- `Email__Mode`
- `Email__Host`
- `Email__Port`
- `Email__EnableSsl`
- `Email__SenderAddress`
- `Email__SenderName`
- `Email__Username`
- `Email__AppPassword`

Never commit their secret values. The public CA certificate is not a private key. `.env*`, `.private-deployment`, build output, database backups and private certificates are excluded from deployment uploads/Git as appropriate.

## Verification performed

- Release build and publish succeeded; CA file exists in publish output.
- Vercel Linux container build succeeded; homepage/login GET returned 200.
- Real hosted student registration succeeded and Gmail SMTP accepted the welcome email. Inbox delivery/appearance must still be checked by the recipient. Passwords are not included in the template.
- Hosted student login, dashboard, course and lesson pages succeeded.
- Hosted administrator login and dashboard/student/course/image-management pages succeeded.
- Both portal cross-access checks and both logout checks passed.
- Temporary hosted test accounts were removed; the original 8 accounts remain.
- Database contains an encrypted Data Protection key record.
- Disposable local integration tests passed migrations/seeding, registration rollback, content CRUD, read-only activity monitoring, attachments/ownership, quiz scoring/progression, CSRF, portal separation, restart persistence, disable/lockout/logout, and safe database-outage handling.

Do not equate local integration tests with a full production final-exam test. Full exam completion, mobile visual review, Gmail inbox delivery, and long-term free-tier availability still require real-user verification.

## Update workflow

Local edits → restore/build/tests → intentional Git commit/push to the existing GitHub main branch → Vercel container build → production deployment. Back up Aiven before future schema changes. Keep development and production credentials separate. GitHub stores code, templates, assets and migration definitions, not live student accounts, passwords, submissions or results.

For a staged CLI release use `npx vercel deploy --prod --skip-domain`, test its deployment URL, then promote only the tested deployment. An old container is not a safe rollback after an incompatible schema migration; this migration is additive.
