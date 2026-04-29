using System;

namespace SASMS.Helpers
{
    public static class AvatarHelper
    {
        public static string GetAvatarUrl(string name, string profilePicturePath = null)
        {
            // If user has uploaded a profile picture, use it
            if (!string.IsNullOrEmpty(profilePicturePath))
            {
                return profilePicturePath;
            }

            // Otherwise, generate a data URI with initial-based avatar
            return GetInitialAvatar(name);
        }

        private static string GetInitialAvatar(string name)
        {
            // Get first letter of name
            string initial = string.IsNullOrEmpty(name) ? "?" : name.Trim().Substring(0, 1).ToUpper();

            // Generate a color based on the name
            var colors = new[]
            {
                "#4D44B5", // Primary purple
                "#4895ef", // Blue
                "#4CAF50", // Green
                "#FCC43E", // Yellow/Orange
                "#FF4D4F", // Red
                "#9C27B0", // Purple
                "#00BCD4", // Cyan
                "#FF9800", // Orange
            };

            int colorIndex = Math.Abs(name?.GetHashCode() ?? 0) % colors.Length;
            string bgColor = colors[colorIndex];

            // Create SVG avatar
            string svg = $@"<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'>
                <rect width='100' height='100' fill='{bgColor}'/>
                <text x='50' y='50' font-family='Poppins, Arial, sans-serif' font-size='45' font-weight='600' fill='white' text-anchor='middle' dominant-baseline='central'>{initial}</text>
            </svg>";

            // Convert to data URI
            string base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
            return $"data:image/svg+xml;base64,{base64}";
        }
    }
}
