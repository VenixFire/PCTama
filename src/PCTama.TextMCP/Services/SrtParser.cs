using PCTama.TextMCP.Models;
using System.Text.RegularExpressions;

namespace PCTama.TextMCP.Services;

public class SrtParser
{
    private static readonly Regex SrtTimestampRegex = new(@"(\d{2}):(\d{2}):(\d{2}),(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2}),(\d{3})", 
        RegexOptions.Compiled);

    public static List<SrtCaption> ParseSrtContent(string content)
    {
        var captions = new List<SrtCaption>();
        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        SrtCaption? currentCaption = null;
        var captionTextLines = new List<string>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // Try to parse as caption index
            if (int.TryParse(line, out int index))
            {
                // Save previous caption if exists
                if (currentCaption != null && captionTextLines.Count > 0)
                {
                    currentCaption.Text = string.Join(" ", captionTextLines);
                    captions.Add(currentCaption);
                    captionTextLines.Clear();
                }
                
                currentCaption = new SrtCaption { Index = index };
            }
            // Try to parse as timestamp
            else if (currentCaption != null && SrtTimestampRegex.IsMatch(line))
            {
                var match = SrtTimestampRegex.Match(line);
                if (match.Success)
                {
                    currentCaption.StartTime = ParseTimeSpan(
                        int.Parse(match.Groups[1].Value),
                        int.Parse(match.Groups[2].Value),
                        int.Parse(match.Groups[3].Value),
                        int.Parse(match.Groups[4].Value));
                    
                    currentCaption.EndTime = ParseTimeSpan(
                        int.Parse(match.Groups[5].Value),
                        int.Parse(match.Groups[6].Value),
                        int.Parse(match.Groups[7].Value),
                        int.Parse(match.Groups[8].Value));
                }
            }
            // Treat as caption text
            else if (currentCaption != null && !string.IsNullOrWhiteSpace(line))
            {
                captionTextLines.Add(line);
            }
        }
        
        // Don't forget the last caption
        if (currentCaption != null && captionTextLines.Count > 0)
        {
            currentCaption.Text = string.Join(" ", captionTextLines);
            captions.Add(currentCaption);
        }
        
        return captions;
    }
    
    private static TimeSpan ParseTimeSpan(int hours, int minutes, int seconds, int milliseconds)
    {
        return new TimeSpan(0, hours, minutes, seconds, milliseconds);
    }
}
