# COLMS production deployment assessment

Status: the Vercel container and Aiven database are configured. See [PRODUCTION-STATUS.md](PRODUCTION-STATUS.md) for current deployment results. The checklist below records the original assessment; it is not the current deployment status.

## Verified platform direction

Vercel's current Docker guide documents Dockerfile.vercel detection and routing HTTP traffic to a container-backed Function:
https://vercel.com/kb/guide/docker

The included multi-stage build preserves ASP.NET Core MVC and targets .NET 10. It listens on PORT (8080 by default) and runs as the .NET image's non-root user. No Next.js conversion or second application is needed. Container build/runtime availability and Hobby eligibility must still be checked in the actual account. A local dotnet publish does not validate a Vercel deployment.

## Blocking requirements before production

- Vercel account/project access and confirmation that the intended academic use meets free-plan terms and quotas. Never select a paid plan automatically.
- Hosted MySQL 8-compatible service with verified TLS. Aiven advertises a free MySQL tier; inspect its current capacity and account eligibility before provisioning: https://aiven.io/docs/products/mysql/concepts/mysql-free-tier
- Durable shared ASP.NET Data Protection keys. The current local key folder cannot survive container replacement or serve multiple instances reliably. Choose a persistent key repository and protect keys at rest before allowing student logins.
- Durable lesson-image upload storage. Activity attachments already live in MySQL, but Admin lesson-image uploads currently write to `src/CPE-Review-Hub/wwwroot/images/lessons/uploads`. A container filesystem is not a production upload store. Migrate these assets to persistent storage and update the existing image service/controller before enabling uploads.
- Configure trusted proxy HTTPS handling for the actual platform. Hosting:LocalProxy trusts loopback only; it is not a generic Vercel proxy configuration. Do not blindly trust arbitrary forwarded headers.
- Add clean student/admin route aliases while keeping old links working; test authorization for both forms. Current entry points remain /student.html and /admin.html.
- Verify Gmail SMTP connectivity from the actual deployment. Never disable certificate validation. Welcome emails must not contain passwords.
- Review Git working tree and history for credentials with redacted output before linking production auto-deploy. Local uncommitted changes are not yet on GitHub.

## Existing database and safe migration

The project uses MySql.EntityFrameworkCore 10.0.9, ApplicationDbContext, and MySQL (not an interchangeable MariaDB configuration). The existing local database is cpe_review_hub. Migrations live in `src/CPE-Review-Hub/Data/Migrations`. Do not drop or reseed the live database to deploy.

1. Schedule a brief write pause and create a consistent logical backup of local MySQL, including schema, data, migration history, and binary activity attachments. Keep the backup outside Git and the Docker build context.
2. Separately inventory and back up uploaded lesson-image files and referenced static assets.
3. Create an empty cloud database. Restore to that database only; do not import server users or grant tables.
4. Compare table row counts, foreign keys, migration history, attachment sizes, and sampled accounts/results against the local source.
5. Run pending EF migrations deliberately against the restored database after backup. Do not run destructive resets or automatic reseeding on container startup.
6. Configure a dedicated production runtime database user, TLS, and secrets in Vercel environment settings. Keep local user-secrets unchanged.
7. Test registration, email, both login areas, content, scoring, progression, uploads, and persistence across a real redeployment before switching users.

Data relationships: users own activity submissions and assessment attempts; submissions own attachments; attempts own answers. Subjects contain lessons; lessons contain materials, images, activities, and questions; assessments link to questions and attempts. Progress/statistics are derived from those records.

## Environment names (values belong in hosting secret settings)

- ASPNETCORE_ENVIRONMENT
- PORT
- ConnectionStrings__ReviewHub
- Application__PublicBaseUrl
- AllowedHosts
- Email__Mode
- Email__Host
- Email__Port
- Email__EnableSsl
- Email__SenderAddress
- Email__SenderName
- Email__Username
- Email__AppPassword

DataProtection__KeysPath currently supports a persistent filesystem only. Do not set it to the developer's Windows path or an ephemeral container path and call it durable. Additional key/object-storage settings depend on the selected persistent services.

## Workflow after blockers are resolved

Intentional reviewed commit -> existing GitHub main branch -> linked Vercel project -> Docker build -> deployment verification -> production domain. Scope production secrets to Production; preview deployments must use isolated test data. Never connect previews to the production student database by default.

MySQL and hosting live independently of the developer PC. GitHub contains source and migrations, not the live database or private uploads. A production URL can only be reported after the provider assigns it and end-to-end tests pass.
