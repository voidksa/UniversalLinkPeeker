using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System;
using System.Text.RegularExpressions;

namespace UniversalLinkPeeker.Services
{
    public class TextExtractionService : IDisposable
    {
        private readonly UIA3Automation _automation;

        public TextExtractionService()
        {
            _automation = new UIA3Automation();
        }

        public string GetUrlUnderMouse(System.Drawing.Point point)
        {
            try
            {
                var element = _automation.FromPoint(point);
                if (element == null) return null;

                // 1. Check ValuePattern
                if (element.Patterns.Value.TryGetPattern(out var valuePattern))
                {
                    string val = valuePattern.Value.Value;
                    if (IsValidUrl(val)) return CleanUrl(val);
                }

                // 2. Check Name
                string name = element.Name;
                if (IsValidUrl(name)) return CleanUrl(name);

                // 3. Legacy IAccessible
                if (element.Patterns.LegacyIAccessible.TryGetPattern(out var legacy))
                {
                    string val = legacy.Value.Value;
                    if (IsValidUrl(val)) return CleanUrl(val);
                    
                    string desc = legacy.Description.Value;
                    if (IsValidUrl(desc)) return CleanUrl(desc);
                }
                
                // 4. Text Pattern (for documents)
                if (element.Patterns.Text.TryGetPattern(out var textPattern))
                {
                    try
                    {
                        var range = textPattern.RangeFromPoint(point);
                        if (range != null)
                        {
                            range.ExpandToEnclosingUnit(TextUnit.Word);
                            string text = range.GetText(500);
                            if (IsValidUrl(text)) return CleanUrl(text);
                        }
                    }
                    catch { }
                }

                // 5. Check Parent (e.g. Text inside Hyperlink)
                var parent = element.Parent;
                if (parent != null && parent.ControlType == ControlType.Hyperlink)
                {
                     if (IsValidUrl(parent.Name)) return CleanUrl(parent.Name);
                     
                     if (parent.Patterns.Value.TryGetPattern(out var parentValue))
                     {
                         if (IsValidUrl(parentValue.Value.Value)) return CleanUrl(parentValue.Value.Value);
                     }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool IsValidUrl(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();

            // Simple check
            if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.TryCreate(text, UriKind.Absolute, out _);
            }
            
            if (text.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.TryCreate("http://" + text, UriKind.Absolute, out _);
            }

            return false;
        }

        private string CleanUrl(string url)
        {
            url = url.Trim();
            if (url.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                return "http://" + url;
            }
            return url;
        }

        public void Dispose()
        {
            _automation?.Dispose();
        }
    }
}
