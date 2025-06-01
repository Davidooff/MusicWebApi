using System.Net;

namespace Application.utils;
public class CookieLoader : List<Cookie>
{
    public CookieLoader(string filePath)
    {
        foreach (string line in File.ReadLines(filePath))
        {
            // Skip comments and empty lines  
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            // Split the line by tabs  
            string[] parts = line.Split('\t');
            if (parts.Length < 7)
                continue;

            string domain = parts[0];
            bool isSecure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            string path = parts[2];
            bool isHttpOnly = parts[1].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            long expires = long.Parse(parts[4]);
            string name = parts[5];
            string value = parts[6];

            // Create a Cookie object  
            Cookie cookie = new(name, value, path, domain)
            {
                Secure = isSecure,
                HttpOnly = isHttpOnly,
                Expires = expires > 0 ? DateTimeOffset.FromUnixTimeSeconds(expires).DateTime : DateTime.MinValue
            };

            Add(cookie);
        }
    }
}

