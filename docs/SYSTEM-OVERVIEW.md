# CPE Review Hub system overview

## Database architecture

- **Server/provider:** MySQL 8 (`MySQL80` locally) through `MySql.EntityFrameworkCore` 10.0.9.
- **Database:** `cpe_review_hub`.
- **DbContext:** `CPE_Review_Hub.Data.ApplicationDbContext`.
- **Connection:** `ConnectionStrings:ReviewHub` is stored in Windows .NET user-secrets, outside Git.
- **Schema:** EF Core migrations in `Data/Migrations`; setup applies pending migrations without recreating the database.

The schema has 18 tables:

- Identity: `AspNetUsers`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`
- Content: `AcademicLevels`, `Subjects`, `Lessons`, `ReviewMaterials`, `LessonActivities`
- Assessments: `Questions`, `Choices`, `Assessments`, `AssessmentQuestions`
- Student records: `ActivitySubmissions`, `SubmissionAttachments`, `AssessmentAttempts`, `AttemptAnswers`
- Schema history: `__EFMigrationsHistory`

## Relationship diagram

```text
AspNetUsers (Student/Admin)
  ├──< ActivitySubmissions >── LessonActivities ──1 Lessons ──< Subjects
  │          └──< SubmissionAttachments              │           │
  │                                                   │           └── AcademicLevels
  └──< AssessmentAttempts >── Assessments >───────────┘
               │                  │
               └──< AttemptAnswers│
                                  └──< AssessmentQuestions >── Questions ──< Choices

Lessons ──< ReviewMaterials
Lessons ──0..1 LessonActivities
Subjects ──< Assessments (lesson quizzes and final exams)
```

`1` means one, `0..1` means optional one, and `<` means many. A Student can submit
one response per activity because `(UserId, LessonActivityId)` is unique. The
submission itself is the completion record. `CompletedAtUtc` records first
completion and `UpdatedAtUtc` records later edits. Attachments belong to the
submission. Activities have no grade, score, feedback, approval, or pass/fail column.

## Learning flow

```text
Read lesson
  → submit icebreaker activity (recorded Completed)
  → lesson quiz unlocks
  → quiz is automatically scored
      ├── below passing percentage: current lesson available; next lesson locked
      └── at/above passing percentage: lesson completed; next lesson unlocked
  → all six lesson quizzes passed
  → final exam unlocks and is automatically scored
```

Admin can edit an icebreaker's title, instructions, expected output, and published
status in the existing lesson editor. Student activity monitoring is read-only,
with Student, Course, Lesson, and date filters plus response/file viewing.
Statistics keep activity participation separate from quiz and exam scores.

## Migration history

1. `InitialAccounts` — Identity users and account fields.
2. `LearningContentFoundation` — levels, subjects, lessons, and materials.
3. `CourseActivitiesAndAssessments` — activities, questions, quizzes, and exams.
4. `StudentLearningProgress` — submissions, attempts, and selected answers.
5. `RenameAcademicLevels` — Sophomore, Junior, and Senior labels.
6. `ActivitySubmissionAttachments` — uploaded activity evidence.
7. `RequireStudentEmailConfirmation` — verified-email requirement.
8. `ActivitySubmissionGrading` — historical grading fields.
9. `RemoveActivityGrading` — removes unused grading fields after confirming the
   live database has zero graded submissions; responses and files remain intact.

## GitHub versus MySQL

GitHub contains application source: models, controllers, services, Razor views,
CSS/JavaScript/images, migrations, scripts, tests, documentation, and package
configuration. Git history records code changes.

MySQL contains runtime data: accounts and password hashes, content rows, activity
responses and attachment bytes, quiz/exam attempts and answers, and migration
history. GitHub does **not** contain student records, submitted files, live scores,
MySQL passwords, Gmail App Passwords, or authentication cookies. Secrets remain
in user-secrets or runtime storage and are excluded from Git.
