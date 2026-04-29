using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SASMS.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string name, string newPassword, string loginUrl)
        {
            try
            {
                var smtpServer = "smtp.gmail.com";
                var smtpPort = 587;
                var fromEmail = "sasms.noreply@gmail.com";
                // IMPORTANT: The user must provide a Google App Password in appsettings.json or environment variables
                var fromPassword = _configuration["Email:Password"] ?? "zdla iowp zofz jsku";

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, "Student Affairs Department");
                message.To.Add(new MailAddress(toEmail));
                message.Subject = "Password Reset - SASMS System";
                message.IsBodyHtml = true;
                
                string emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f9f9f9; }}
        .container {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.05); }}
        .header {{ background: linear-gradient(135deg, #4D44B5 0%, #764ba2 100%); padding: 30px; text-align: center; color: white; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 600; letter-spacing: 1px; }}
        .content {{ padding: 40px; }}
        .welcome {{ font-size: 18px; font-weight: 600; color: #4D44B5; margin-bottom: 20px; }}
        .instruction {{ color: #555; margin-bottom: 30px; }}
        .password-card {{ background-color: #f0f4f8; border-left: 4px solid #4D44B5; padding: 25px; border-radius: 8px; text-align: center; margin-bottom: 30px; }}
        .password-label {{ font-size: 12px; text-transform: uppercase; color: #777; letter-spacing: 1px; margin-bottom: 8px; }}
        .password-value {{ font-family: 'Courier New', Courier, monospace; font-size: 28px; font-weight: 700; color: #d32f2f; letter-spacing: 2px; }}
        .action-btn {{ display: inline-block; padding: 12px 30px; background-color: #4D44B5; color: #ffffff !important; text-decoration: none; border-radius: 6px; font-weight: 600; margin-top: 10px; }}
        .warning {{ background-color: #fff9c4; padding: 15px; border-radius: 6px; font-size: 13px; color: #856404; margin-top: 25px; display: flex; align-items: center; border: 1px solid #ffeeba; }}
        .footer {{ background-color: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #777; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>SASMS SYSTEM</h1>
        </div>
        <div class='content'>
            <div class='welcome'>Dear {name},</div>
            <p class='instruction'>Your password reset request has been reviewed and <strong>approved</strong> by the school administration.</p>
            
            <div class='password-card'>
                <div class='password-label'>Your New Access Password</div>
                <div class='password-value'>{newPassword}</div>
                <p style='margin-bottom: 0; font-size: 13px; color: #666;'>Use this temporary password to access your account.</p>
            </div>

            <div style='text-align: center;'>
                <a href='{loginUrl}' class='action-btn'>Login to Portal</a>
            </div>

            <div class='warning'>
                <span><strong>Security Note:</strong> For your safety, please change this password immediately after logging in for the first time.</span>
            </div>
            
            <p style='margin-top: 30px;'>Regards,<br><strong>Student Affairs Department</strong><br>Eva International Applied Technology School</p>
        </div>
        <div class='footer'>
            <p>&copy; 2026 SASMS System. All Rights Reserved.</p>
            <p>This is an automated message, please do not reply directly to this email.</p>
        </div>
    </div>
</body>
</html>";

                message.Body = emailBody;

                using var client = new SmtpClient(smtpServer, smtpPort);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(fromEmail, fromPassword);

                await client.SendMailAsync(message);
                _logger.LogInformation("Password reset email sent to {Email}", toEmail);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendAdmissionStatusEmailAsync(string toEmail, string name, bool isAccepted, string studentId = null, string className = null)
        {
            try
            {
                var smtpServer = "smtp.gmail.com";
                var smtpPort = 587;
                var fromEmail = "sasms.noreply@gmail.com";
                var fromPassword = _configuration["Email:Password"] ?? "zdla iowp zofz jsku";

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, "Admissions Department - SASMS");
                message.To.Add(new MailAddress(toEmail));
                message.Subject = isAccepted ? "Congratulations! Admission Accepted - SASMS" : "Application Status Update - SASMS";
                message.IsBodyHtml = true;

                string primaryColor = isAccepted ? "#2ecc71" : "#64748b";
                string accentColor = isAccepted ? "#4D44B5" : "#1e293b";
                string headerBg = isAccepted ? "linear-gradient(135deg, #4D44B5 0%, #2ecc71 100%)" : "linear-gradient(135deg, #1e293b 0%, #64748b 100%)";

                string statusIcon = isAccepted ? "🎉" : "✉️";
                string title = isAccepted ? "Congratulations, and Welcome!" : "Thank You for Your Interest";
                
                string contentHtml = isAccepted 
                    ? $@"
                        <div class='welcome'>Welcome to the SASMS Family, {name}!</div>
                        <p class='instruction'>We are thrilled to inform you that your application to <strong>Eva International Applied Technology School</strong> has been <strong>approved</strong>. We were very impressed with your performance during the screening process.</p>
                        
                        <div class='info-card' style='border-left: 4px solid #2ecc71;'>
                            <div class='info-label'>Your Official Student ID</div>
                            <div class='info-value' style='color: #2ecc71;'>{studentId}</div>
                            <p style='margin: 10px 0 0 0; font-size: 13px; color: #666;'><strong>Assigned Class:</strong> {className}</p>
                        </div>

                        <p>Our student affairs department will contact you shortly with information regarding the orientation day and the start of the academic year.</p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='https://sasms-portal.edu/Student/Dashboard' class='action-btn' style='background-color: #4D44B5;'>Access Student Portal</a>
                        </div>"
                    : $@"
                        <div class='welcome' style='color: #1e293b;'>Dear {name},</div>
                        <p class='instruction'>Thank you for your interest in joining <strong>Eva International Applied Technology School</strong> and for taking the time to participate in our admission process.</p>
                        
                        <div class='info-card' style='border-left: 4px solid #64748b; background-color: #f8fafc; text-align: left;'>
                            <p style='margin: 0; color: #475569;'>We regret to inform you that we are unable to offer you admission at this time. Due to the limited number of seats and the high volume of applications, we had to make some very difficult choices.</p>
                        </div>

                        <p>We appreciated learning about your background and achievements, and we wish you the very best of luck in your future academic endeavors.</p>";

                string emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Inter', 'Segoe UI', Helvetica, Arial, sans-serif; line-height: 1.6; color: #334155; margin: 0; padding: 0; background-color: #f1f5f9; }}
        .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.05); }}
        .header {{ background: {headerBg}; padding: 40px; text-align: center; color: white; }}
        .status-badge {{ font-size: 40px; margin-bottom: 10px; display: block; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px; }}
        .content {{ padding: 45px; }}
        .welcome {{ font-size: 20px; font-weight: 700; color: {accentColor}; margin-bottom: 20px; }}
        .instruction {{ color: #475569; margin-bottom: 30px; line-height: 1.7; }}
        .info-card {{ background-color: #f8fafc; padding: 25px; border-radius: 12px; margin-bottom: 30px; text-align: center; }}
        .info-label {{ font-size: 11px; text-transform: uppercase; color: #64748b; font-weight: 700; letter-spacing: 1.5px; margin-bottom: 10px; }}
        .info-value {{ font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace; font-size: 32px; font-weight: 800; letter-spacing: 2px; }}
        .action-btn {{ display: inline-block; padding: 14px 32px; color: #ffffff !important; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 15px; transition: 0.2s; }}
        .footer {{ background-color: #f8fafc; padding: 30px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
        .footer p {{ margin: 6px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <span class='status-badge'>{statusIcon}</span>
            <h1>{title}</h1>
        </div>
        <div class='content'>
            {contentHtml}
            
            <p style='margin-top: 40px; border-top: 1px solid #f1f5f9; padding-top: 20px;'>
                Regards,<br>
                <strong>Admissions Team</strong><br>
                Eva International Applied Technology School
            </p>
        </div>
        <div class='footer'>
            <p>SASMS Portal &copy; 2026. All Rights Reserved.</p>
            <p>123 School Avenue, Cairo, Egypt</p>
            <p style='margin-top: 15px; color: #cbd5e1;'>This is an automated system notification.</p>
        </div>
    </div>
</body>
</html>";

                message.Body = emailBody;

                using var client = new SmtpClient(smtpServer, smtpPort);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(fromEmail, fromPassword);

                await client.SendMailAsync(message);
                _logger.LogInformation("Admission status email ({Status}) sent to {Email}", isAccepted ? "Accepted" : "Rejected", toEmail);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send admission email to {Email}", toEmail);
                // We don't throw here to avoid failing the whole transaction if email fails, 
                // but for critical onboarding it's better to log properly.
            }
        }
    }
}
