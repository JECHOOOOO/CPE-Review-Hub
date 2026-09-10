# COLMS security and operations

## Runtime architecture

Production requests flow from the browser to the Vercel ASP.NET Core container and then to Aiven MySQL over TLS. Static files committed under `wwwroot` ship with the container. Uploaded lesson images and activity attachments are stored in MySQL so they survive a container replacement.

Development uses the same application with a local MySQL connection stored in .NET user-secrets. Production settings are supplied by Vercel environment variables. Never put secret values in this document or `appsettings*.json`.

## Required production configuration names

- `ConnectionStrings__ReviewHub`
- `DataProtection__PersistToDatabase`
- `DataProtection__CertificateBase64`
- `Application__PublicBaseUrl`
- `Email__Mode`
- `Email__Host`
- `Email__Port`
- `Email__EnableSsl`
- `Email__SenderAddress`
- `Email__SenderName`
- `Email__Username`
- `Email__AppPassword`

## Health and monitoring

- `GET /health/live` checks that the web process is responding.
- `GET /health` or `/health/ready` checks whether the application can reach its configured database. It returns HTTP 200 with `Healthy`, or HTTP 503 with `Unhealthy`. It never returns a host, username, exception, or connection string.
- Use a free HTTPS uptime monitor against `/health` at a modest interval such as five minutes. A monitor cannot guarantee that an Aiven Free service will never pause.
- Review Vercel deployment and runtime logs for startup failures, HTTP 5xx responses, slow requests, failed authentication patterns, and email/database warnings.
- Review the Aiven Overview and Metrics pages for service state, storage, CPU, memory, connections, and database availability.

Logs intentionally omit passwords, tokens, cookies, connection strings, email addresses, student names, and submitted answers. Login events record only the portal and outcome. Slow-request logs include only method, path, status, and elapsed time.

## Deployment gate

The GitHub workflow `.github/workflows/build-and-test.yml` restores, builds the Release application, runs focused unit tests, and publishes an artifact. Vercel should deploy from `main` only after this workflow succeeds. The repository does not contain a Vercel production token.

## Safe database migrations and backup

An application deployment is not a database backup. Before a production migration:

1. Review the generated migration for dropped or narrowed columns, destructive SQL, and cascade-delete changes.
2. Export or snapshot the Aiven database and verify that the backup is usable.
3. Apply the migration with the dedicated migration/admin credential, not the restricted runtime account.
4. Check `/health`, login, representative reads/writes, and logs.
5. Keep the local development database and the pre-change backup.

Do not use `EnsureDeleted`, delete migration history, or rebuild production to solve a schema mismatch. Aiven Free plan backup and restore guarantees are more limited than paid plans; keep a separate periodic encrypted logical export outside Git and test restoration.

## Authorization and data ownership

Student and Admin use separate cookie schemes and policies. Student-facing queries derive `UserId` from the authenticated `NameIdentifier` claim. Attempt results and attachments also filter by that value, preventing a student from changing an ID in the URL to read another student's record. Every Admin controller requires the Admin policy. Activities remain participation-only; quiz and final-exam results are calculated automatically.

COLMS currently has no separate JSON business API. MVC controllers and forms remain the correct interface. Any future API must use the same policies, ownership filters, antiforgery or an appropriate token scheme, validation, rate limits, and safe error responses.

## Incident checks

When users cannot sign in, check `/health` first. HTTP 503 indicates database availability/configuration rather than a deleted account. If health is good, check the correct Student/Admin portal, account status, lockout, and credentials without asking the user to disclose a password. For registration failures, inspect sanitized SMTP and transaction logs; COLMS rolls back account creation if the welcome email cannot be accepted.
