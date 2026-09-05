"""Integration tests against a disposable loopback-only MySQL 8 instance.

Uses only Python's standard library, installed MySQL binaries, and .NET.
Never connects to the user's MySQL service or reads their connection secrets.
Run from the repository: python tests/verify_mysql.py
"""
import argparse
import base64
import html
import http.cookiejar
import os
from pathlib import Path
import re
import secrets
import shutil
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
DLL = ROOT / "bin/Release/net10.0/CPE-Review-Hub.dll"
HIDDEN = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0


def free_port():
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


class Client:
    def __init__(self, base):
        self.base = base
        self.cookies = http.cookiejar.CookieJar()
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(self.cookies), NoRedirect())

    def request(self, path, data=None):
        encoded = None if data is None else urllib.parse.urlencode(data).encode()
        req = urllib.request.Request(self.base + path, data=encoded)
        try:
            response = self.opener.open(req, timeout=10)
        except urllib.error.HTTPError as error:
            response = error
        return response.code, response.read().decode(errors="replace"), response.headers

    def token(self, path):
        status, body, _ = self.request(path)
        assert status == 200, f"GET {path}: {status}"
        match = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', body)
        assert match, f"No CSRF token on {path}"
        return html.unescape(match.group(1))

    def post(self, path, data, source=None):
        return self.request(path, {**data, "__RequestVerificationToken": self.token(source or path)})

    def multipart_post(self, path, data, files, source=None):
        boundary = "----CpeReviewHub" + secrets.token_hex(12)
        parts = []
        fields = {**data, "__RequestVerificationToken": self.token(source or path)}
        for name, value in fields.items():
            parts.append((f"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n{value}\r\n").encode())
        for name, filename, content_type, content in files:
            parts.append((f"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"; filename=\"{filename}\"\r\nContent-Type: {content_type}\r\n\r\n").encode() + content + b"\r\n")
        parts.append(f"--{boundary}--\r\n".encode())
        req = urllib.request.Request(self.base + path, data=b"".join(parts),
            headers={"Content-Type": f"multipart/form-data; boundary={boundary}"})
        try:
            response = self.opener.open(req, timeout=10)
        except urllib.error.HTTPError as error:
            response = error
        return response.code, response.read(), response.headers


def verify(mysql_bin):
    work_parent = (ROOT / "obj" / "integration").resolve()
    work_parent.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix="mysql-", dir=work_parent)).resolve()
    assert work.is_relative_to(work_parent)
    db_process = app_process = None
    database_ready = False
    handles = []
    db_port, app_port = free_port(), free_port()
    while app_port == db_port:
        app_port = free_port()
    root_password = "Root!" + secrets.token_hex(24)
    app_password = "App!" + secrets.token_hex(24)
    password = "Student!" + secrets.token_hex(12)
    simple_password = "abcdefgh"
    admin_password = "Admin!" + secrets.token_hex(12)
    env = os.environ.copy()
    # Keep disposable antiforgery/data-protection keys inside the disposable test folder.
    env["DataProtection__KeysPath"] = str(work / "keys")
    env["Email__Mode"] = "Pickup"
    env["Email__PickupDirectory"] = str(work / "emails")
    env["ASPNETCORE_ENVIRONMENT"] = "Development"
    env["Logging__LogLevel__Default"] = "Warning"
    env["Logging__EventLog__LogLevel__Default"] = "None"
    env["ConnectionStrings__ReviewHub"] = (
        f"Server=127.0.0.1;Port={db_port};Database=cpe_integration;"
        # This disposable loopback-only server has no TLS certificate. The real
        # development connection remains in user-secrets and is not changed.
        f"User ID=cpe_test;Password={app_password};SslMode=Disabled;AllowPublicKeyRetrieval=True;ConnectionTimeout=5")

    def sql(statement, authenticated=True):
        sql_env = os.environ.copy()
        sql_env.pop("MYSQL_PWD", None)
        if authenticated:
            sql_env["MYSQL_PWD"] = root_password
        result = subprocess.run([str(mysql_bin / "mysql.exe"), "--no-defaults", "--host=127.0.0.1",
            f"--port={db_port}", "--user=root", "--batch", "--skip-column-names"],
            input=statement, text=True, capture_output=True, env=sql_env, creationflags=HIDDEN, timeout=15)
        assert result.returncode == 0, "Isolated test SQL failed (credentials omitted)."
        return result.stdout.strip()

    def command(*args, input=None):
        result = subprocess.run(["dotnet", str(DLL), *args], cwd=ROOT, env=env, input=input,
            text=True, capture_output=True, creationflags=HIDDEN, timeout=180)
        assert result.returncode == 0, f"App command {args} failed: {result.stdout[-900:]} {result.stderr[-900:]}"

    def start_app():
        output = open(work / f"app-{time.time_ns()}.log", "w", encoding="utf-8")
        handles.append(output)
        return subprocess.Popen(["dotnet", str(DLL), "--urls", f"http://127.0.0.1:{app_port}"],
            cwd=ROOT, env=env, stdout=output, stderr=subprocess.STDOUT, creationflags=HIDDEN)

    def wait_app(client):
        deadline = time.monotonic() + 20
        while time.monotonic() < deadline:
            try:
                if client.request("/")[0] == 200:
                    return
            except OSError:
                pass
            time.sleep(.25)
        raise AssertionError("Test app did not become ready.")

    def login(client, portal, email, secret, extra=None):
        return client.post(f"/{portal}.html", {"Email": email, "Password": secret, **(extra or {})})

    try:
        print("Initializing isolated MySQL data directory...", flush=True)
        init = subprocess.run([str(mysql_bin / "mysqld.exe"), "--no-defaults", "--initialize-insecure",
            f"--basedir={mysql_bin.parent}", f"--datadir={work / 'data'}"], capture_output=True,
            text=True, timeout=60, creationflags=HIDDEN)
        assert init.returncode == 0, "Temporary MySQL initialization failed."
        output = open(work / "mysql.log", "w", encoding="utf-8")
        handles.append(output)
        db_process = subprocess.Popen([str(mysql_bin / "mysqld.exe"), "--no-defaults",
            f"--basedir={mysql_bin.parent}", f"--datadir={work / 'data'}", f"--port={db_port}",
            "--bind-address=127.0.0.1", "--mysqlx=OFF", "--innodb-buffer-pool-size=32M", "--console"],
            stdout=output, stderr=subprocess.STDOUT, creationflags=HIDDEN)
        for _ in range(80):
            try:
                sql("SELECT 1;", authenticated=False)
                break
            except AssertionError:
                if db_process.poll() is not None:
                    raise AssertionError("Temporary MySQL process exited.")
                time.sleep(.25)
        else:
            raise AssertionError("Temporary MySQL startup timed out.")
        sql(f"ALTER USER 'root'@'localhost' IDENTIFIED BY '{root_password}';"
            "CREATE DATABASE cpe_integration CHARACTER SET utf8mb4;"
            f"CREATE USER 'cpe_test'@'127.0.0.1' IDENTIFIED BY '{app_password}';"
            "GRANT ALL ON cpe_integration.* TO 'cpe_test'@'127.0.0.1';", authenticated=False)
        database_ready = True
        command("--initialize-database")
        command("--initialize-database")
        command("--seed-course-catalog")
        command("--seed-course-catalog")
        assert sql("SELECT COUNT(*) FROM cpe_integration.Subjects;") == "34"
        assert sql("SELECT GROUP_CONCAT(CONCAT(AcademicLevelId, ':', CourseCount) ORDER BY AcademicLevelId SEPARATOR ',') "
            "FROM (SELECT AcademicLevelId, COUNT(*) CourseCount FROM cpe_integration.Subjects GROUP BY AcademicLevelId) levels;") == "200:11,300:16,400:7"
        assert sql("SELECT COUNT(*) FROM cpe_integration.Lessons;") == "204"
        assert sql("SELECT COUNT(*) FROM cpe_integration.ReviewMaterials WHERE CHAR_LENGTH(Content) > 1500;") == "204"
        assert sql("SELECT COUNT(*) FROM cpe_integration.LessonImages WHERE ImagePath LIKE '/images/lessons/%' AND AltText<>'';") == "204"
        assert sql("SELECT COUNT(*) FROM cpe_integration.LessonActivities WHERE Instructions LIKE '%Required process:%' OR ExpectedOutput LIKE '%50 characters%';") == "0"
        assert sql("SELECT COUNT(*) FROM cpe_integration.LessonActivities;") == "204"
        assert sql("SELECT COUNT(*) FROM cpe_integration.Assessments WHERE Type=1 AND RequiredQuestionCount=10;") == "204"
        assert sql("SELECT COUNT(*) FROM cpe_integration.Assessments WHERE Type=2 AND RequiredQuestionCount=50;") == "34"
        assert sql("SELECT COUNT(*) FROM cpe_integration.Questions WHERE SourceKey IS NOT NULL AND IsActive=1;") == "2040"
        assert sql("SELECT COUNT(*) FROM cpe_integration.Choices;") == "8160"
        assert sql("SELECT COUNT(*) FROM cpe_integration.Assessments WHERE IsPublished=1;") == "238"
        assert sql("SELECT COUNT(*) FROM cpe_integration.AssessmentQuestions aq JOIN cpe_integration.Assessments a ON a.Id=aq.AssessmentId WHERE a.Type=1;") == "2040"
        assert sql("SELECT COUNT(*) FROM cpe_integration.AssessmentQuestions aq JOIN cpe_integration.Assessments a ON a.Id=aq.AssessmentId WHERE a.Type=2;") == "1700"
        assert sql("SELECT COUNT(*) FROM cpe_integration.Lessons WHERE Title IN ('Orientation, Scope, and Foundations','Core Concepts and Representations');") == "0"
        assert sql("SELECT Title FROM cpe_integration.Subjects WHERE Code='CPE Elec 2';") == "System Maintenance and Administration 2"
        assert sql("SELECT Title FROM cpe_integration.Subjects WHERE Code='CPE 530';") == "Engineering Ethics"
        command("--create-admin", input=f"admin@example.test\nTest Administrator\n{admin_password}\n{admin_password}\n")
        print("PASS: Migrations, idempotent 34-course catalog, six-lesson structure, and CLI admin provisioning.", flush=True)
        app_process = start_app()
        base = f"http://127.0.0.1:{app_port}"
        anonymous, student, admin, second = [Client(base) for _ in range(4)]
        wait_app(anonymous)
        for path in ("/", "/student.html", "/admin.html", "/Student/Account/Register", "/Home/Privacy", "/css/hub.css"):
            assert anonymous.request(path)[0] == 200, path
        for path in ("/", "/student.html", "/Student/Account/Register"):
            assert "/admin.html" not in anonymous.request(path)[1], f"Admin link exposed on {path}"
        for area in ("Student", "Admin"):
            status, _, headers = anonymous.request(f"/{area}/Dashboard")
            assert status == 302 and f"/{area.lower()}.html" in headers["Location"]
        assert anonymous.request("/Student/Account/Register", {"Email": "csrf@example.test"})[0] == 400
        assert anonymous.request("/admin.html", {"Email": "csrf@example.test"})[0] == 400
        for client, email, registration_password in (
            (student, "student@example.test", password),
            (second, "second@example.test", simple_password)):
            email_files_before = set((work / "emails").glob("*.eml"))
            status, _, _ = client.post("/Student/Account/Register", {"Email": email,
                "FullName": "Test Student", "Password": registration_password,
                "ConfirmPassword": registration_password,
                "AccountType": "Admin", "IsActive": "false"})
            assert status == 302, "Student registration failed"
            new_email_files = set((work / "emails").glob("*.eml")) - email_files_before
            assert len(new_email_files) == 1, "Verification email was not generated"
            email_content = next(iter(new_email_files)).read_text(encoding="utf-8")
            assert f"To: {email}" in email_content and "Verify your CPE Review Hub account" in email_content
            confirmation_link = html.unescape(re.search(r'https?://[^\"\s<]+/student/confirm-email\?[^\"\s<]+', email_content).group(0))
            confirmation_url = urllib.parse.urlparse(confirmation_link)
            if email == "student@example.test":
                assert login(client, "student", email, registration_password)[0] == 200
            confirm_status, confirm_body, _ = client.request(confirmation_url.path + "?" + confirmation_url.query)
            assert confirm_status == 200 and "Email verified" in confirm_body
        assert sql("SELECT COUNT(*) FROM cpe_integration.AspNetUsers WHERE AccountType='Student' AND IsActive=1;") == "2"
        assert sql("SELECT COUNT(*) FROM cpe_integration.AspNetUsers WHERE AccountType='Student' AND EmailConfirmed=1;") == "2"
        assert sql("SELECT COUNT(*) FROM cpe_integration.AspNetUsers WHERE PasswordHash IS NOT NULL AND LENGTH(PasswordHash)>60;") == "3"
        duplicate = student.post("/Student/Account/Register", {"Email": "STUDENT@example.test",
            "FullName": "Duplicate", "Password": password, "ConfirmPassword": password})
        assert duplicate[0] == 200 and "Unable to register" in duplicate[1]
        assert login(student, "student", "student@example.test", "Wrong!" + secrets.token_hex(12))[0] == 200
        assert login(student, "admin", "student@example.test", password)[0] == 200
        assert login(admin, "student", "admin@example.test", admin_password)[0] == 200
        status, _, headers = login(student, "student", "student@example.test", password,
            {"ReturnUrl": "https://example.invalid/steal"})
        assert status == 302 and headers["Location"].startswith("/Student")
        assert "httponly" in headers.get("Set-Cookie", "").lower()
        dashboard_status, dashboard_body, _ = student.request("/Student/Dashboard")
        assert dashboard_status == 200
        assert "Sophomore" in dashboard_body and "Junior" in dashboard_body and "Senior" in dashboard_body
        assert student.request("/Admin/Dashboard")[0] == 302
        assert student.request("/Admin/Students")[0] == 302
        first_lesson_id = sql("SELECT l.Id FROM cpe_integration.Lessons l JOIN cpe_integration.Subjects s ON s.Id=l.SubjectId WHERE s.Code='CPE 211' AND l.SortOrder=1;")
        first_subject_id = sql("SELECT Id FROM cpe_integration.Subjects WHERE Code='CPE 211';")
        second_lesson_id = sql("SELECT l.Id FROM cpe_integration.Lessons l JOIN cpe_integration.Subjects s ON s.Id=l.SubjectId WHERE s.Code='CPE 211' AND l.SortOrder=2;")
        first_activity_id = sql("SELECT la.Id FROM cpe_integration.LessonActivities la WHERE la.LessonId=" + first_lesson_id + ";")
        first_quiz_id = sql("SELECT a.Id FROM cpe_integration.Assessments a WHERE a.LessonId=" + first_lesson_id + " AND a.Type=1;")
        lesson_path = f"/student/lessons/{first_lesson_id}"
        lesson_body = student.request(lesson_path)[1]
        assert "Arrays and Linked Lists" in lesson_body and "Array access by index is O(1)" in lesson_body
        assert "/images/lessons/cpe211/lesson-1.svg" in lesson_body and "<figcaption>" in lesson_body
        assert "minlength=\"50\"" not in lesson_body and "Required process:" not in lesson_body
        for course_code, image_code in (("CPE 213", "cpe213"), ("CPE 312", "cpe312"), ("CPE 323", "cpe323"), ("CPE 416", "cpe416")):
            lesson_id_for_image = sql("SELECT l.Id FROM cpe_integration.Lessons l JOIN cpe_integration.Subjects s ON s.Id=l.SubjectId "
                f"WHERE s.Code='{course_code}' AND l.SortOrder=1;")
            page = student.request(f"/student/lessons/{lesson_id_for_image}")[1]
            assert f"/images/lessons/{image_code}/lesson-1.svg" in page and "<figcaption>" in page
        assert anonymous.request("/images/lessons/cpe211/lesson-1.svg")[0] == 200
        assert student.request(f"/student/lessons/{second_lesson_id}")[0] == 302
        sample_png = base64.b64decode("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=")
        assert student.multipart_post(f"/student/activities/{first_activity_id}/submit", {
            "Response": "Array"
        }, [("Attachments", "array-trace.png", "image/png", sample_png)], lesson_path)[0] == 302
        attachment_id = sql("SELECT Id FROM cpe_integration.SubmissionAttachments LIMIT 1;")
        assert student.request(f"/student/activities/attachments/{attachment_id}")[0] == 200
        assert student.multipart_post(f"/student/activities/{first_activity_id}/submit", {
            "Response": "This update must be rejected because executable attachments are not allowed by the activity submission policy."
        }, [("Attachments", "unsafe.exe", "application/octet-stream", b"MZ-not-allowed")], lesson_path)[0] == 302
        assert sql("SELECT COUNT(*) FROM cpe_integration.SubmissionAttachments;") == "1"
        status, _, headers = student.post(f"/student/assessments/{first_quiz_id}/start", {}, lesson_path)
        assert status == 302 and "/student/assessments/attempts/" in headers["Location"]
        take_path = urllib.parse.urlparse(headers["Location"]).path
        attempt_id = take_path.rsplit("/", 1)[-1]
        take_body = student.request(take_path)[1]
        assert take_body.count('question-card') == 10
        assert "Question 1 of 10" in take_body and "choice-letter" in take_body and "0 of 10 answered" in take_body
        choice_rows = sql("SELECT aq.QuestionId,MAX(CASE WHEN c.IsCorrect=1 THEN c.Id END),"
            "MIN(CASE WHEN c.IsCorrect=0 THEN c.Id END) FROM cpe_integration.AssessmentQuestions aq "
            "JOIN cpe_integration.Choices c ON c.QuestionId=aq.QuestionId "
            f"WHERE aq.AssessmentId={first_quiz_id} GROUP BY aq.QuestionId,aq.SortOrder ORDER BY aq.SortOrder;").splitlines()
        failed_answers = {}
        for index, row in enumerate(choice_rows):
            question, correct, incorrect = row.split(chr(9))
            failed_answers[f"Answers[{question}]"] = correct if index < 6 else incorrect
        failed_answers["AttemptId"] = attempt_id
        status, _, headers = student.post(f"{take_path}/submit", failed_answers, take_path)
        assert status == 302 and headers["Location"].endswith("/result")
        result_body = student.request(urllib.parse.urlparse(headers["Location"]).path)[1]
        assert "6 / 10" in result_body and "60%" in result_body and "Try again" in result_body
        assert sql("SELECT COUNT(*) FROM cpe_integration.ActivitySubmissions;") == "1"
        assert student.request(f"/student/lessons/{second_lesson_id}")[0] == 302
        status, _, headers = student.post(f"/student/assessments/{first_quiz_id}/start", {}, lesson_path)
        assert status == 302 and "/student/assessments/attempts/" in headers["Location"]
        take_path = urllib.parse.urlparse(headers["Location"]).path
        attempt_id = take_path.rsplit("/", 1)[-1]
        answer_data = {f"Answers[{row.split(chr(9))[0]}]": row.split(chr(9))[1] for row in choice_rows}
        answer_data["AttemptId"] = attempt_id
        status, _, headers = student.post(f"{take_path}/submit", answer_data, take_path)
        result_body = student.request(urllib.parse.urlparse(headers["Location"]).path)[1]
        assert "10 / 10" in result_body and "100%" in result_body and "Correct" in result_body
        assert ">Attempt<" in result_body and ">#2<" in result_body
        assert student.request(f"/student/lessons/{second_lesson_id}")[0] == 200
        second_lesson_body = student.request(f"/student/lessons/{second_lesson_id}")[1]
        assert "MatchingResponses[0]" in second_lesson_body and "/images/lessons/cpe211/lesson-2.svg" in second_lesson_body
        assert "Data Structures And Algorithms" in student.request("/student/progress")[1]
        assert sql("SELECT COUNT(*) FROM cpe_integration.ActivitySubmissions;") == "1"
        assert sql("SELECT COUNT(*) FROM cpe_integration.SubmissionAttachments WHERE FileName='array-trace.png' AND FileSize>0;") == "1"
        assert sql("SELECT COUNT(*) FROM cpe_integration.AssessmentAttempts WHERE Score=10 AND IsPassed=1;") == "1"
        assert sql("SELECT COUNT(*) FROM cpe_integration.AssessmentAttempts WHERE Score=6 AND IsPassed=0;") == "1"
        assert login(admin, "admin", "admin@example.test", admin_password)[0] == 302
        assert admin.request("/Admin/Dashboard")[0] == 200
        assert admin.request("/Student/Dashboard")[0] == 302
        assert admin.request("/Student/Profile")[0] == 302
        assert admin.request("/admin/assessments?type=LessonQuiz")[0] == 200
        assert admin.request("/admin/assessments?type=FinalExam")[0] == 200
        assert admin.request("/admin/results")[0] == 200
        assert admin.request("/admin/statistics")[0] == 200
        assert admin.request(f"/admin/assessments/{first_quiz_id}")[0] == 200
        assert admin.request(f"/admin/questions/create?assessmentId={first_quiz_id}")[0] == 200
        assert admin.request(f"/admin/results/{attempt_id}")[0] == 200
        submission_id = sql("SELECT Id FROM cpe_integration.ActivitySubmissions LIMIT 1;")
        assert admin.request(f"/admin/activity-submissions/{submission_id}")[0] == 200
        assert admin.request(f"/admin/activity-submissions/attachments/{attachment_id}")[0] == 200
        review_path = f"/admin/activity-submissions/{submission_id}"
        grade_path = review_path + "/grade"
        preview_path = f"/admin/activity-submissions/attachments/{attachment_id}/preview"
        activity_list = admin.request("/admin/activity-submissions")[1]
        assert "Completed" in activity_list and "View" in activity_list and "Grade" not in activity_list
        filtered_activities = admin.request(f"/admin/activity-submissions?student=student%40example.test&courseId={first_subject_id}&lessonId={first_lesson_id}")[1]
        assert "array-trace.png" in filtered_activities or "Arrays and Linked Lists" in filtered_activities
        review_body = admin.request(review_path)[1]
        assert "array-trace.png" in review_body and "READ-ONLY PARTICIPATION RECORD" in review_body
        assert "Grade this activity" not in review_body and "Save grade" not in review_body
        assert admin.request(preview_path)[0] == 200
        assert student.request(preview_path)[0] == 302
        assert anonymous.request(review_path)[0] == 302
        rejected_grade_status = admin.post(grade_path, {"Grade.Score": "90"}, review_path)[0]
        assert rejected_grade_status in (400, 404, 405), rejected_grade_status
        columns = sql("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='cpe_integration' "
            "AND table_name='ActivitySubmissions' AND column_name IN ('Grade','Feedback','GradedAtUtc','GradedById','Revision');")
        assert columns == "0"
        assert student.post(f"/student/activities/{first_activity_id}/submit", {
            "Response": "I updated the linked-list trace and documented the empty head pointer, insertion links, and boundary cases carefully."
        }, lesson_path)[0] == 302
        assert "Activity completed" in student.request(lesson_path)[1]
        assert login(second, "student", "second@example.test", simple_password)[0] == 302
        assert second.request(f"/student/activities/attachments/{attachment_id}")[0] == 404
        assert second.post(f"/student/activities/attachments/{attachment_id}/delete", {}, "/Student/Profile")[0] == 404
        assert student.post(f"/student/activities/attachments/{attachment_id}/delete", {}, lesson_path)[0] == 302
        assert "Activity participation" in admin.request("/admin/statistics")[1]
        print("PASS: Admin activity monitoring is read-only with filters, attachments, ownership, and participation-only statistics.", flush=True)
        assert admin.post("/admin/questions/save", {"AssessmentId": first_quiz_id,
            "LessonId": first_lesson_id, "Prompt": "Which access pattern is taught for an array?",
            "Explanation": "Arrays support direct indexed access.", "ChoiceA": "Direct indexed access",
            "ChoiceB": "Pointer-only traversal", "ChoiceC": "Network routing", "ChoiceD": "Disk scheduling",
            "CorrectChoiceIndex": "0", "IsActive": "true"},
            f"/admin/questions/create?assessmentId={first_quiz_id}")[0] == 302
        added_question_id = sql("SELECT MAX(q.Id) FROM cpe_integration.Questions q WHERE q.SourceKey IS NULL;")
        assert sql(f"SELECT COUNT(*) FROM cpe_integration.AssessmentQuestions WHERE AssessmentId={first_quiz_id};") == "11"
        assert admin.post(f"/admin/questions/{added_question_id}/delete", {"assessmentId": first_quiz_id},
            f"/admin/questions/{added_question_id}/delete?assessmentId={first_quiz_id}")[0] == 302
        assert sql(f"SELECT COUNT(*) FROM cpe_integration.AssessmentQuestions WHERE AssessmentId={first_quiz_id};") == "10"
        print("PASS: Verified-email registration, activity gating, automatic quiz scoring, progress, CSRF, and portal separation.", flush=True)
        assert student.request("/admin/subjects")[0] == 302
        assert admin.request("/student/subjects")[0] == 302
        assert admin.post("/admin/subjects/save", {"Code": "TEST 999", "Title": "Temporary Integration Course",
            "Description": "Published integration subject", "AcademicLevelId": "200",
            "IsPublished": "true"}, "/admin/subjects/create")[0] == 302
        subject_id = sql("SELECT Id FROM cpe_integration.Subjects WHERE Code='TEST 999';")
        assert admin.post("/admin/subjects/save", {"Id": subject_id, "Code": "TEST 999",
            "Title": "Updated Temporary Integration Course", "Description": "Published integration subject",
            "AcademicLevelId": "200", "IsPublished": "true"}, f"/admin/subjects/{subject_id}/edit")[0] == 302
        assert admin.post("/admin/lessons/save", {"SubjectId": subject_id, "Title": "Digital Foundations",
            "Summary": "Start with digital systems.", "SortOrder": "1", "IsPublished": "true",
            "ActivityTitle": "Binary icebreaker", "ActivityInstructions": "Compare binary and hexadecimal representations in a short worked response.",
            "ActivityExpectedOutput": "A conversion table with a concise explanation.", "ActivityResponseType": "1", "ActivityIsPublished": "true"},
            f"/admin/lessons/create?subjectId={subject_id}")[0] == 302
        lesson_id = sql("SELECT Id FROM cpe_integration.Lessons WHERE SubjectId=" + subject_id + ";")
        assert sql("SELECT COUNT(*) FROM cpe_integration.LessonActivities WHERE LessonId=" + lesson_id + " AND Title='Binary icebreaker' AND IsPublished=1;") == "1"
        assert admin.post("/admin/materials/save", {"LessonId": lesson_id, "Title": "Number Systems",
            "Content": "Binary and hexadecimal review material.", "SortOrder": "1", "IsPublished": "true"},
            f"/admin/materials/create?lessonId={lesson_id}")[0] == 302
        status, subject_body, _ = student.request("/student/subjects")
        assert status == 200 and "CPE 211" in subject_body and "TEST 999" in subject_body
        assert "Sophomore" in subject_body and "Junior" in subject_body and "Senior" in subject_body
        assert "Digital Foundations" in student.request(f"/student/subjects/{subject_id}")[1]
        assert "Binary and hexadecimal" in student.request(f"/student/lessons/{lesson_id}")[1]
        assert admin.post("/admin/subjects/save", {"Id": subject_id, "Code": "TEST 999",
            "Title": "Hidden Temporary Integration Course", "AcademicLevelId": "200"},
            f"/admin/subjects/{subject_id}/edit")[0] == 302
        assert "TEST 999" not in student.request("/student/subjects")[1]
        assert student.request(f"/student/subjects/{subject_id}")[0] == 404
        assert admin.post(f"/admin/subjects/{subject_id}/delete", {},
            f"/admin/subjects/{subject_id}/delete")[0] == 302
        assert sql("SELECT COUNT(*) FROM cpe_integration.Subjects;") == "34"
        print("PASS: Exact grouped catalog, Admin CRUD, draft filtering, activity content, and route isolation.", flush=True)
        assert student.post("/Student/Profile", {"FullName": "Saved Profile", "University": "Test College",
            "AccountType": "Admin", "Id": "another-user"})[0] == 302
        assert sql("SELECT FullName FROM cpe_integration.AspNetUsers WHERE Email='second@example.test';") == "Test Student"
        app_process.terminate()
        app_process.wait(timeout=10)
        app_process = start_app()
        wait_app(anonymous)
        assert "Saved Profile" in student.request("/Student/Profile")[1]
        assert "Activity completed" in student.request(lesson_path)[1]
        assert "Saved Profile" in admin.request("/Admin/Students")[1]
        student_id = sql("SELECT Id FROM cpe_integration.AspNetUsers WHERE Email='student@example.test';")
        assert admin.request(f"/Admin/Students/Progress?id={student_id}")[0] == 200
        assert admin.post("/Admin/Students/SetActive", {"id": student_id, "active": "false"}, "/Admin/Students")[0] == 302
        assert student.request("/Student/Dashboard")[0] == 302
        assert login(student, "student", "student@example.test", password)[0] == 200
        assert admin.post("/Admin/Students/SetActive", {"id": student_id, "active": "true"}, "/Admin/Students")[0] == 302
        assert login(student, "student", "student@example.test", password)[0] == 302
        assert student.post("/Student/Account/Logout", {}, "/Student/Dashboard")[0] == 302
        assert student.request("/Student/Dashboard")[0] == 302
        for _ in range(5):
            assert login(second, "student", "second@example.test", "Wrong!" + secrets.token_hex(12))[0] == 200
        assert login(second, "student", "second@example.test", simple_password)[0] == 200
        assert sql("SELECT COUNT(*) FROM cpe_integration.AspNetUsers WHERE Email='second@example.test' AND LockoutEnd IS NOT NULL;") == "1"
        assert admin.post("/Admin/Account/Logout", {}, "/Admin/Dashboard")[0] == 302
        assert admin.request("/Admin/Dashboard")[0] == 302
        print("PASS: Persistence across restart, profile ownership, account disable, lockout, and logout.", flush=True)
        # MySQL on Windows can have a supervising parent; use SQL shutdown so
        # the worker flushes and releases its files as well.
        sql("SHUTDOWN;")
        db_process.wait(timeout=15)
        database_ready = False
        status, body, _ = login(student, "student", "student@example.test", password)
        assert status == 503 and "Database connection needed" in body and app_password not in body, f"Outage response: {status}"
        print("PASS: MySQL outage returns a safe 503 page, without exposing credentials.", flush=True)
    finally:
        if database_ready:
            try:
                sql("SHUTDOWN;")
                db_process.wait(timeout=20)
            except (AssertionError, subprocess.TimeoutExpired):
                pass
        for process in (app_process, db_process):
            if process and process.poll() is None:
                process.terminate()
                process.wait(timeout=20)
        for handle in handles:
            handle.close()
        # Only remove the uniquely-created test directory, never a service data directory.
        if work.is_relative_to(work_parent) and work.name.startswith("mysql-"):
            shutil.rmtree(work)
        print("Temporary test processes/data cleaned up; existing MySQL service untouched.", flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--mysql-bin", type=Path,
        default=Path("C:/Program Files/MySQL/MySQL Server 8.0/bin"))
    args = parser.parse_args()
    verify(args.mysql_bin)
