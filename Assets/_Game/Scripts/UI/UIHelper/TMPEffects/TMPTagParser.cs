using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Types of custom inline effects supported by the TMP effect system.
    /// </summary>
    public enum TMPEffectType
    {
        Wave,
        Shake,
        Rainbow
    }

    /// <summary>
    /// Represents a range of TMP character indices that should receive a specific effect.
    /// </summary>
    [System.Serializable]
    public struct TagRange
    {
        public TMPEffectType EffectType;
        public int StartIndex; // inclusive
        public int EndIndex;   // exclusive

        public TagRange(TMPEffectType effectType, int startIndex, int endIndex)
        {
            EffectType = effectType;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public bool Contains(int charIndex)
        {
            return charIndex >= StartIndex && charIndex < EndIndex;
        }
    }

    /// <summary>
    /// Parses custom rich text tags (wave, shake, rainbow) from TMP text.
    /// Strips custom tags while preserving standard TMP tags, and returns
    /// character index ranges mapped to TMP's characterInfo indices.
    /// </summary>
    public static class TMPTagParser
    {
        private static readonly string[] CustomTagNames = { "wave", "shake", "rainbow" };

        private static readonly Dictionary<string, TMPEffectType> TagToEffect = new()
        {
            { "wave", TMPEffectType.Wave },
            { "shake", TMPEffectType.Shake },
            { "rainbow", TMPEffectType.Rainbow }
        };

        /// <summary>
        /// Parse custom tags from raw text, strip them, and return character ranges.
        /// Standard TMP tags are preserved. Returned indices correspond to TMP characterInfo indices.
        /// </summary>
        public static string Parse(string rawText, out List<TagRange> ranges)
        {
            ranges = new List<TagRange>();

            if (string.IsNullOrEmpty(rawText))
                return rawText ?? string.Empty;

            var output = new System.Text.StringBuilder(rawText.Length);
            var activeEffects = new Stack<(TMPEffectType type, int startCharIndex)>();
            int charIndex = 0; // TMP visible character index
            int i = 0;

            while (i < rawText.Length)
            {
                if (rawText[i] == '<')
                {
                    // Check for custom closing tag first: </wave>, </shake>, </rainbow>
                    if (TryMatchClosingTag(rawText, i, out string closingTagName, out int closingTagEnd))
                    {
                        if (activeEffects.Count > 0 && activeEffects.Peek().type == TagToEffect[closingTagName])
                        {
                            var active = activeEffects.Pop();
                            ranges.Add(new TagRange(active.type, active.startCharIndex, charIndex));
                        }
                        i = closingTagEnd;
                        continue;
                    }

                    // Check for custom opening tag: <wave>, <shake>, <rainbow>
                    if (TryMatchOpeningTag(rawText, i, out string openingTagName, out int openingTagEnd))
                    {
                        activeEffects.Push((TagToEffect[openingTagName], charIndex));
                        i = openingTagEnd;
                        continue;
                    }

                    // Standard TMP tag — preserve it but don't count as visible characters
                    int tagClose = rawText.IndexOf('>', i);
                    if (tagClose != -1)
                    {
                        // Append the entire TMP tag as-is
                        int tagLen = tagClose - i + 1;
                        output.Append(rawText, i, tagLen);
                        i = tagClose + 1;
                        continue;
                    }
                }

                // Regular visible character
                output.Append(rawText[i]);
                charIndex++;
                i++;
            }

            // Close any unclosed tags at end of text
            while (activeEffects.Count > 0)
            {
                var active = activeEffects.Pop();
                ranges.Add(new TagRange(active.type, active.startCharIndex, charIndex));
            }

            return output.ToString();
        }

        private static bool TryMatchOpeningTag(string text, int pos, out string tagName, out int endPos)
        {
            tagName = null;
            endPos = pos;

            foreach (string name in CustomTagNames)
            {
                // Match <tagname> exactly
                string tag = $"<{name}>";
                if (pos + tag.Length <= text.Length &&
                    string.Compare(text, pos, tag, 0, tag.Length, System.StringComparison.OrdinalIgnoreCase) == 0)
                {
                    tagName = name;
                    endPos = pos + tag.Length;
                    return true;
                }
            }

            return false;
        }

        private static bool TryMatchClosingTag(string text, int pos, out string tagName, out int endPos)
        {
            tagName = null;
            endPos = pos;

            foreach (string name in CustomTagNames)
            {
                string tag = $"</{name}>";
                if (pos + tag.Length <= text.Length &&
                    string.Compare(text, pos, tag, 0, tag.Length, System.StringComparison.OrdinalIgnoreCase) == 0)
                {
                    tagName = name;
                    endPos = pos + tag.Length;
                    return true;
                }
            }

            return false;
        }
    }
}
