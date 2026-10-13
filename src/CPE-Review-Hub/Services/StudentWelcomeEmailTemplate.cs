using System.Net;

namespace CPE_Review_Hub.Services;

public static class StudentWelcomeEmailTemplate
{
    public const string Subject = "Welcome to COLMS – Your Account Is Ready";

    public static string Build(string fullName, string email, string signInUrl)
    {
        var firstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Student";
        var safeFirstName = WebUtility.HtmlEncode(firstName);
        var safeEmail = WebUtility.HtmlEncode(email);
        var safeSignInUrl = WebUtility.HtmlEncode(signInUrl);

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{Subject}}</title>
              <style>
                @media only screen and (max-width: 600px) {
                  .page { padding: 16px 8px !important; }
                  .header, .content, .footer { padding-left: 22px !important; padding-right: 22px !important; }
                  .heading { font-size: 24px !important; }
                  .button { display: block !important; text-align: center !important; }
                }
              </style>
            </head>
            <body style="margin:0;padding:0;background:#f1f6fa;color:#172b4d;font-family:Arial,'Helvetica Neue',sans-serif;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" class="page" style="width:100%;background:#f1f6fa;padding:32px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="width:100%;max-width:620px;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 28px rgba(16,42,67,.10);">
                      <tr>
                        <td class="header" style="background:#0b2948;border-bottom:4px solid #27c2d1;padding:28px 36px;text-align:center;">
                          <div style="font-size:30px;line-height:1;font-weight:700;letter-spacing:2px;color:#ffffff;">COLMS</div>
                          <div style="margin-top:9px;font-size:14px;line-height:1.5;color:#bfeaf0;">CentOs Learning Management System</div>
                        </td>
                      </tr>
                      <tr>
                        <td class="content" style="padding:38px 42px 34px;">
                          <h1 class="heading" style="margin:0 0 18px;color:#0b2948;font-size:28px;line-height:1.25;">Welcome to COLMS, {{safeFirstName}}!</h1>
                          <p style="margin:0 0 16px;color:#40546d;font-size:16px;line-height:1.65;">Your student account has been successfully created and is ready to use.</p>
                          <p style="margin:0 0 26px;color:#40546d;font-size:16px;line-height:1.65;">You can now sign in to COLMS to access your courses, lessons, activities, quizzes, examinations, and learning progress.</p>

                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="width:100%;background:#f4f9fc;border-left:4px solid #27c2d1;border-radius:8px;">
                            <tr>
                              <td style="padding:22px 24px;">
                                <div style="margin-bottom:17px;color:#16788a;font-size:12px;font-weight:700;letter-spacing:1.4px;">YOUR ACCOUNT</div>
                                <div style="color:#6a7b90;font-size:12px;font-weight:700;text-transform:uppercase;">Email</div>
                                <div style="margin-top:5px;color:#172b4d;font-size:16px;line-height:1.5;word-break:break-word;">{{safeEmail}}</div>
                                <div style="margin-top:17px;color:#6a7b90;font-size:12px;font-weight:700;text-transform:uppercase;">Password</div>
                                <div style="margin-top:5px;color:#172b4d;font-size:16px;line-height:1.5;">Use the password you created during registration.</div>
                              </td>
                            </tr>
                          </table>

                          <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin:28px auto;">
                            <tr>
                              <td style="border-radius:999px;background:#0b6580;">
                                <a class="button" href="{{safeSignInUrl}}" style="display:inline-block;padding:14px 28px;color:#ffffff;font-size:14px;font-weight:700;letter-spacing:.5px;text-decoration:none;">SIGN IN TO COLMS</a>
                              </td>
                            </tr>
                          </table>

                          <p style="margin:0 0 13px;color:#40546d;font-size:14px;line-height:1.6;">You can use your COLMS account to sign in from any supported device.</p>
                          <p style="margin:0;color:#6a7b90;font-size:13px;line-height:1.6;">If you did not create this account or believe this email was sent to you by mistake, please contact the COLMS administrator.</p>
                        </td>
                      </tr>
                      <tr>
                        <td class="footer" style="background:#f7fafc;border-top:1px solid #e4edf3;padding:24px 36px;text-align:center;">
                          <div style="color:#0b2948;font-size:15px;font-weight:700;">COLMS</div>
                          <div style="margin-top:4px;color:#6a7b90;font-size:12px;">CentOs Learning Management System</div>
                          <div style="margin-top:12px;color:#8a98aa;font-size:11px;line-height:1.5;">This is an automated message. Please do not reply to this email.</div>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
