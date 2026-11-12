using System;

public static class TagHelper
{
    public const string TitleTag = "Title";
    private const string OpenTag = "<";
    private const string CloseTag = ">";
    private const string SeparatorTag = "</";

    public static string GetTitle(string original, string tag)
    {
        var t = GetContentWithTags(original, tag);
        if (!t.Contains(OpenTag + tag + CloseTag)) return "";
        t = t.Replace(OpenTag + tag + CloseTag, "");
        t = t.Replace(SeparatorTag + tag + CloseTag, "");
        return t;
    }

    private static string GetContentWithTags(string originalContent, string tag)
    {
        if (string.IsNullOrEmpty(originalContent))
            return "";

        if (!originalContent.Contains(OpenTag + tag + CloseTag)) return "";
        var start = originalContent.IndexOf(OpenTag + tag + CloseTag, StringComparison.Ordinal);
        var end = originalContent.IndexOf(SeparatorTag + tag + CloseTag, start, StringComparison.Ordinal) + ((SeparatorTag + tag + CloseTag).Length);
        var length = end - start;
        var t = originalContent.Substring(start, length);
        return t;
    }
    public static string GetSlideWithoutTitle(string original, string tag)
    {
        if (string.IsNullOrEmpty(original))
            return "";

        var t = GetContentWithTags(original, tag);
        if (!string.IsNullOrEmpty(t))
        {
            original = original.Replace(t, "");
        }
        return original;
    }
}