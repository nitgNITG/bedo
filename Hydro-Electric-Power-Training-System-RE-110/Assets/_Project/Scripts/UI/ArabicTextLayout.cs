using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;

// Shapes logical Arabic and orders each wrapped line while preserving Latin terminal codes.
public static class ArabicTextLayout
{
    private static readonly Dictionary<char, char[]> Forms = new Dictionary<char, char[]>
    {
        { '\u0671', new char[] { '\uFB50', '\uFB51', '\u0000', '\u0000' } },
        { '\u067B', new char[] { '\uFB52', '\uFB53', '\uFB54', '\uFB55' } },
        { '\u067E', new char[] { '\uFB56', '\uFB57', '\uFB58', '\uFB59' } },
        { '\u0680', new char[] { '\uFB5A', '\uFB5B', '\uFB5C', '\uFB5D' } },
        { '\u067A', new char[] { '\uFB5E', '\uFB5F', '\uFB60', '\uFB61' } },
        { '\u067F', new char[] { '\uFB62', '\uFB63', '\uFB64', '\uFB65' } },
        { '\u0679', new char[] { '\uFB66', '\uFB67', '\uFB68', '\uFB69' } },
        { '\u06A4', new char[] { '\uFB6A', '\uFB6B', '\uFB6C', '\uFB6D' } },
        { '\u06A6', new char[] { '\uFB6E', '\uFB6F', '\uFB70', '\uFB71' } },
        { '\u0684', new char[] { '\uFB72', '\uFB73', '\uFB74', '\uFB75' } },
        { '\u0683', new char[] { '\uFB76', '\uFB77', '\uFB78', '\uFB79' } },
        { '\u0686', new char[] { '\uFB7A', '\uFB7B', '\uFB7C', '\uFB7D' } },
        { '\u0687', new char[] { '\uFB7E', '\uFB7F', '\uFB80', '\uFB81' } },
        { '\u068D', new char[] { '\uFB82', '\uFB83', '\u0000', '\u0000' } },
        { '\u068C', new char[] { '\uFB84', '\uFB85', '\u0000', '\u0000' } },
        { '\u068E', new char[] { '\uFB86', '\uFB87', '\u0000', '\u0000' } },
        { '\u0688', new char[] { '\uFB88', '\uFB89', '\u0000', '\u0000' } },
        { '\u0698', new char[] { '\uFB8A', '\uFB8B', '\u0000', '\u0000' } },
        { '\u0691', new char[] { '\uFB8C', '\uFB8D', '\u0000', '\u0000' } },
        { '\u06A9', new char[] { '\uFB8E', '\uFB8F', '\uFB90', '\uFB91' } },
        { '\u06AF', new char[] { '\uFB92', '\uFB93', '\uFB94', '\uFB95' } },
        { '\u06B3', new char[] { '\uFB96', '\uFB97', '\uFB98', '\uFB99' } },
        { '\u06B1', new char[] { '\uFB9A', '\uFB9B', '\uFB9C', '\uFB9D' } },
        { '\u06BA', new char[] { '\uFB9E', '\uFB9F', '\u0000', '\u0000' } },
        { '\u06BB', new char[] { '\uFBA0', '\uFBA1', '\uFBA2', '\uFBA3' } },
        { '\u06C0', new char[] { '\uFBA4', '\uFBA5', '\u0000', '\u0000' } },
        { '\u06C1', new char[] { '\uFBA6', '\uFBA7', '\uFBA8', '\uFBA9' } },
        { '\u06BE', new char[] { '\uFBAA', '\uFBAB', '\uFBAC', '\uFBAD' } },
        { '\u06D2', new char[] { '\uFBAE', '\uFBAF', '\u0000', '\u0000' } },
        { '\u06D3', new char[] { '\uFBB0', '\uFBB1', '\u0000', '\u0000' } },
        { '\u06AD', new char[] { '\uFBD3', '\uFBD4', '\uFBD5', '\uFBD6' } },
        { '\u06C7', new char[] { '\uFBD7', '\uFBD8', '\u0000', '\u0000' } },
        { '\u06C6', new char[] { '\uFBD9', '\uFBDA', '\u0000', '\u0000' } },
        { '\u06C8', new char[] { '\uFBDB', '\uFBDC', '\u0000', '\u0000' } },
        { '\u0677', new char[] { '\uFBDD', '\u0000', '\u0000', '\u0000' } },
        { '\u06CB', new char[] { '\uFBDE', '\uFBDF', '\u0000', '\u0000' } },
        { '\u06C5', new char[] { '\uFBE0', '\uFBE1', '\u0000', '\u0000' } },
        { '\u06C9', new char[] { '\uFBE2', '\uFBE3', '\u0000', '\u0000' } },
        { '\u06D0', new char[] { '\uFBE4', '\uFBE5', '\uFBE6', '\uFBE7' } },
        { '\u0649', new char[] { '\uFEEF', '\uFEF0', '\uFBE8', '\uFBE9' } },
        { '\u06CC', new char[] { '\uFBFC', '\uFBFD', '\uFBFE', '\uFBFF' } },
        { '\u0621', new char[] { '\uFE80', '\u0000', '\u0000', '\u0000' } },
        { '\u0622', new char[] { '\uFE81', '\uFE82', '\u0000', '\u0000' } },
        { '\u0623', new char[] { '\uFE83', '\uFE84', '\u0000', '\u0000' } },
        { '\u0624', new char[] { '\uFE85', '\uFE86', '\u0000', '\u0000' } },
        { '\u0625', new char[] { '\uFE87', '\uFE88', '\u0000', '\u0000' } },
        { '\u0626', new char[] { '\uFE89', '\uFE8A', '\uFE8B', '\uFE8C' } },
        { '\u0627', new char[] { '\uFE8D', '\uFE8E', '\u0000', '\u0000' } },
        { '\u0628', new char[] { '\uFE8F', '\uFE90', '\uFE91', '\uFE92' } },
        { '\u0629', new char[] { '\uFE93', '\uFE94', '\u0000', '\u0000' } },
        { '\u062A', new char[] { '\uFE95', '\uFE96', '\uFE97', '\uFE98' } },
        { '\u062B', new char[] { '\uFE99', '\uFE9A', '\uFE9B', '\uFE9C' } },
        { '\u062C', new char[] { '\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0' } },
        { '\u062D', new char[] { '\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4' } },
        { '\u062E', new char[] { '\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8' } },
        { '\u062F', new char[] { '\uFEA9', '\uFEAA', '\u0000', '\u0000' } },
        { '\u0630', new char[] { '\uFEAB', '\uFEAC', '\u0000', '\u0000' } },
        { '\u0631', new char[] { '\uFEAD', '\uFEAE', '\u0000', '\u0000' } },
        { '\u0632', new char[] { '\uFEAF', '\uFEB0', '\u0000', '\u0000' } },
        { '\u0633', new char[] { '\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4' } },
        { '\u0634', new char[] { '\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8' } },
        { '\u0635', new char[] { '\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC' } },
        { '\u0636', new char[] { '\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0' } },
        { '\u0637', new char[] { '\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4' } },
        { '\u0638', new char[] { '\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8' } },
        { '\u0639', new char[] { '\uFEC9', '\uFECA', '\uFECB', '\uFECC' } },
        { '\u063A', new char[] { '\uFECD', '\uFECE', '\uFECF', '\uFED0' } },
        { '\u0641', new char[] { '\uFED1', '\uFED2', '\uFED3', '\uFED4' } },
        { '\u0642', new char[] { '\uFED5', '\uFED6', '\uFED7', '\uFED8' } },
        { '\u0643', new char[] { '\uFED9', '\uFEDA', '\uFEDB', '\uFEDC' } },
        { '\u0644', new char[] { '\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0' } },
        { '\u0645', new char[] { '\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4' } },
        { '\u0646', new char[] { '\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8' } },
        { '\u0647', new char[] { '\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC' } },
        { '\u0648', new char[] { '\uFEED', '\uFEEE', '\u0000', '\u0000' } },
        { '\u064A', new char[] { '\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4' } },
    };
    public static bool ContainsArabic(string s) => !string.IsNullOrEmpty(s) && s.Any(c => c >= 0x0600 && c <= 0x06ff);
    private static bool Mark(char c) => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark;
    public static string Shape(string text)
    {
        char[] result = text.ToCharArray();
        for (int i=0; i<text.Length; i++)
        {
            if (!Forms.TryGetValue(text[i], out var f)) continue;
            int p=i-1, n=i+1;
            while(p>=0 && Mark(text[p])) p--;
            while(n<text.Length && Mark(text[n])) n++;
            bool before=p>=0 && Forms.TryGetValue(text[p],out var pf) && pf[2]!=0 && f[1]!=0;
            bool after=n<text.Length && Forms.TryGetValue(text[n],out var nf) && f[2]!=0 && nf[1]!=0;
            result[i]=before && after && f[3]!=0 ? f[3] : before && f[1]!=0 ? f[1] : after && f[2]!=0 ? f[2] : f[0];
        }
        return new string(result);
    }
    public static string Visual(string shaped)
    {
        return string.Join("\n", shaped.Replace("\r", "").Split('\n').Select(Line));
    }
    private static string Line(string text)
    {
        // Treat each Latin/number run as a single token before reversing the line.
        var tokens=Regex.Matches(text, @"[A-Za-z0-9][A-Za-z0-9 ./+%:_-]*|\P{M}\p{M}*")
            .Cast<Match>().Select(m=>m.Value).ToList();
        tokens.Reverse(); var output=new StringBuilder();
        foreach(string token in tokens)
        {
            if(token.Length==1) output.Append(token=="("?")":token==")"?"(":token=="["?"]":token=="]"?"[":token);
            else output.Append(token);
        }
        return output.ToString();
    }
    public static string Render(string logical, TMP_Text target, float availableWidth = 0)
    {
        // Translation prose is plain text. Keep rich-text rotation used on the card tabs.
        bool rotated=logical.Contains("<rotate=");
        string plain=Regex.Replace(logical,"<[^>]*>","");
        if(rotated)return "<rotate=90>"+Visual(Shape(plain))+"</rotate>";
        // TextMesh Pro receives visual order here. Keep the title separator
        // to the left of the digits, and the shaped experiment label to their right.
        var experimentTitle = Regex.Match(plain, @"^التجربة\s+(\d+)\s*:\s*(.+)$");
        if (!rotated && experimentTitle.Success)
            return Line(Shape(experimentTitle.Groups[2].Value)) + " : " +
                experimentTitle.Groups[1].Value + " " + Line(Shape("التجربة"));
        float width=Math.Max(1,(availableWidth > 0 ? availableWidth : target.rectTransform.rect.width)-target.margin.x-target.margin.z);
        var lines=new List<string>();
        foreach(string paragraph in plain.Replace("\r", "").Split('\n'))
        {
            string line="";
            foreach(string word in paragraph.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate=line.Length==0?word:line+" "+word;
                if(line.Length>0 && target.GetPreferredValues(Shape(candidate),float.PositiveInfinity,float.PositiveInfinity).x>width)
                { lines.Add(Line(Shape(line))); line=word; }
                else line=candidate;
            }
            lines.Add(Line(Shape(line)));
        }
        string result=string.Join("\n",lines);
        return rotated?"<rotate=90>"+result+"</rotate>":result;
    }
}
