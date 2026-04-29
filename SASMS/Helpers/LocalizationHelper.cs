using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;

namespace SASMS.Helpers
{
    public static class LocalizationHelper
    {
        public static string TranslateNotification(string message, IStringLocalizer localizer)
        {
            if (string.IsNullOrEmpty(message)) return message;

            // 1. Password Reset Request: "Student {name} has requested a password reset."
            var pwdResetRegex = new Regex(@"Student (.+) has requested a password reset\.");
            var match = pwdResetRegex.Match(message);
            if (match.Success)
            {
                string name = match.Groups[1].Value;
                return string.Format(localizer["Notification_PasswordResetRequest"], name);
            }

            // 2. New Application: "New application received from {name}. Application #: {number}"
            var appRegex = new Regex(@"New application received from (.+)\. Application #: (.+)");
            match = appRegex.Match(message);
            if (match.Success)
            {
                string name = match.Groups[1].Value;
                string appNum = match.Groups[2].Value;
                return string.Format(localizer["Notification_NewApplication"], name, appNum);
            }

            // 3. New Message: "You have a new message from {name}"
            var msgRegex = new Regex(@"You have a new message from (.+)");
            match = msgRegex.Match(message);
            if (match.Success)
            {
                string name = match.Groups[1].Value;
                return string.Format(localizer["Notification_NewMessage"], name);
            }

            // Fallback to localizer for exact matches or return as is
            return localizer[message];
        }
    }
}
