using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WikiFunctions.Plugin;

namespace AAccentFix.Awb
{
    // Classes fusionnées depuis AAccentFix.Core pour éviter les dépendances
    public sealed class FixResult
    {
        public string Text { get; }
        public int ReplacementCount { get; }
        public IReadOnlyList<string> AppliedRules { get; }

        public FixResult(string text, int replacementCount, IReadOnlyList<string> appliedRules)
        {
            Text = text;
            ReplacementCount = replacementCount;
            AppliedRules = appliedRules;
        }
    }

    public sealed class AccentRule
    {
        public string WrongPhrase { get; }
        public string CorrectPhrase { get; }
        public Regex Pattern { get; }

        public AccentRule(string wrongPhrase, string correctPhrase)
        {
            if (string.IsNullOrWhiteSpace(wrongPhrase)) throw new ArgumentException(nameof(wrongPhrase));
            if (string.IsNullOrWhiteSpace(correctPhrase)) throw new ArgumentException(nameof(correctPhrase));

            WrongPhrase = wrongPhrase;
            CorrectPhrase = correctPhrase;
            Pattern = BuildPattern(wrongPhrase);
        }

        private static Regex BuildPattern(string phrase)
        {
            var words = phrase.Split(' ');
            var sb = new StringBuilder();
            sb.Append(@"\b");

            for (int w = 0; w < words.Length; w++)
            {
                if (w > 0) sb.Append(@"\s+");

                string word = words[w];
                for (int c = 0; c < word.Length; c++)
                {
                    char ch = word[c];
                    if (ch == '\'' || ch == '\u2019')
                    {
                        sb.Append("['\u2019]");
                    }
                    else
                    {
                        sb.Append(Regex.Escape(ch.ToString()));
                    }
                }
            }

            sb.Append(@"\b");
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public string Apply(Match match)
        {
            bool firstLetterUpper = match.Value.Length > 0 && char.IsUpper(match.Value[0]);
            if (!firstLetterUpper) return CorrectPhrase;

            return char.ToUpperInvariant(CorrectPhrase[0]) + CorrectPhrase.Substring(1);
        }
    }

    public sealed class TextMasker
    {
        private const char TokenStart = '\uE000';
        private const char TokenEnd = '\uE001';

        public class MaskResult
        {
            public string MaskedText { get; }
            public List<string> ProtectedSpans { get; }

            public MaskResult(string maskedText, List<string> protectedSpans)
            {
                MaskedText = maskedText;
                ProtectedSpans = protectedSpans;
            }
        }

        public MaskResult Mask(string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var protectedSpans = new List<string>();
            var output = new StringBuilder(input.Length);

            int i = 0;
            int n = input.Length;

            while (i < n)
            {
                // 1. Commentaires HTML <!-- ... -->
                if (Match(input, i, "<!--"))
                {
                    int end = input.IndexOf("-->", i, StringComparison.Ordinal);
                    int spanEnd = end < 0 ? n : end + 3;
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 2. <nowiki>...</nowiki>
                if (MatchTagOpen(input, i, "nowiki"))
                {
                    int spanEnd = FindTagEnd(input, i, "nowiki");
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 3. <ref ...>...</ref>
                if (MatchTagOpen(input, i, "ref"))
                {
                    int spanEnd = FindTagEnd(input, i, "ref");
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 4. <math>...</math> et <syntaxhighlight>...</syntaxhighlight>
                if (MatchTagOpen(input, i, "math"))
                {
                    int spanEnd = FindTagEnd(input, i, "math");
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }
                if (MatchTagOpen(input, i, "syntaxhighlight"))
                {
                    int spanEnd = FindTagEnd(input, i, "syntaxhighlight");
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 5. Modèles {{ ... }} avec gestion de l'imbrication (protège toutes les citations)
                if (Match(input, i, "{{"))
                {
                    int spanEnd = FindBalancedEnd(input, i, "{{", "}}");
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 6. Liens internes [[ ... ]]
                if (Match(input, i, "[["))
                {
                    int spanEnd = FindBalancedEnd(input, i, "[[", "]]");
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 7. Liens externes [http://... texte]
                if (input[i] == '[' && (Match(input, i + 1, "http://") || Match(input, i + 1, "https://")))
                {
                    int end = input.IndexOf(']', i);
                    int spanEnd = end < 0 ? n : end + 1;
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 8. Gras+italique '''''...'''''
                if (Match(input, i, "'''''") && NotMatchChar(input, i - 1, '\'') && NotMatchChar(input, i + 5, '\''))
                {
                    int end = input.IndexOf("'''''", i + 5);
                    if (end >= 0 && NotMatchChar(input, end - 1, '\'') && NotMatchChar(input, end + 5, '\''))
                    {
                        int spanEnd = end + 5;
                        Emit(output, protectedSpans, input, i, spanEnd);
                        i = spanEnd;
                        continue;
                    }
                }

                // 9. Italique ''...''
                if (Match(input, i, "''") && NotMatchChar(input, i - 1, '\'') && NotMatchChar(input, i + 2, '\''))
                {
                    int end = input.IndexOf("''", i + 2);
                    if (end >= 0 && NotMatchChar(input, end - 1, '\'') && NotMatchChar(input, end + 2, '\''))
                    {
                        int spanEnd = end + 2;
                        Emit(output, protectedSpans, input, i, spanEnd);
                        i = spanEnd;
                        continue;
                    }
                }

                // 10. Guillemets français « ... »
                if (input[i] == '«')
                {
                    int end = input.IndexOf('»', i);
                    if (end >= 0)
                    {
                        int spanEnd = end + 1;
                        Emit(output, protectedSpans, input, i, spanEnd);
                        i = spanEnd;
                        continue;
                    }
                }

                output.Append(input[i]);
                i++;
            }

            return new MaskResult(output.ToString(), protectedSpans);
        }

        public string Unmask(string maskedText, List<string> protectedSpans)
        {
            if (maskedText == null) throw new ArgumentNullException(nameof(maskedText));
            if (protectedSpans == null) throw new ArgumentNullException(nameof(protectedSpans));

            var sb = new StringBuilder(maskedText.Length);
            int i = 0;
            int n = maskedText.Length;

            while (i < n)
            {
                if (maskedText[i] == TokenStart)
                {
                    int end = maskedText.IndexOf(TokenEnd, i + 1);
                    if (end < 0)
                    {
                        sb.Append(maskedText[i]);
                        i++;
                        continue;
                    }

                    string indexStr = maskedText.Substring(i + 1, end - i - 1);
                    if (int.TryParse(indexStr, out int idx) && idx >= 0 && idx < protectedSpans.Count)
                    {
                        sb.Append(protectedSpans[idx]);
                    }
                    else
                    {
                        sb.Append(maskedText.Substring(i, end - i + 1));
                    }
                    i = end + 1;
                }
                else
                {
                    sb.Append(maskedText[i]);
                    i++;
                }
            }

            return sb.ToString();
        }

        private static void Emit(StringBuilder output, List<string> protectedSpans, string input, int start, int end)
        {
            string span = input.Substring(start, end - start);
            int index = protectedSpans.Count;
            protectedSpans.Add(span);
            output.Append(TokenStart).Append(index).Append(TokenEnd);
        }

        private static bool Match(string input, int pos, string literal)
        {
            if (pos < 0 || pos + literal.Length > input.Length) return false;
            return string.CompareOrdinal(input, pos, literal, 0, literal.Length) == 0;
        }

        private static bool NotMatchChar(string input, int pos, char c)
        {
            if (pos < 0 || pos >= input.Length) return true;
            return input[pos] != c;
        }

        private static bool MatchTagOpen(string input, int pos, string tagName)
        {
            if (pos >= input.Length || input[pos] != '<') return false;
            int p = pos + 1;
            int t = 0;
            while (t < tagName.Length)
            {
                if (p >= input.Length || char.ToLowerInvariant(input[p]) != char.ToLowerInvariant(tagName[t]))
                    return false;
                p++; t++;
            }
            if (p >= input.Length) return false;
            char next = input[p];
            return next == '>' || next == '/' || char.IsWhiteSpace(next);
        }

        private static int FindTagEnd(string input, int start, string tagName)
        {
            int n = input.Length;
            int openTagClose = input.IndexOf('>', start);
            if (openTagClose < 0) return n;

            if (input[openTagClose - 1] == '/')
            {
                return openTagClose + 1;
            }

            string closeTag = "</" + tagName + ">";
            int closeIdx = IndexOfIgnoreCase(input, closeTag, openTagClose + 1);
            return closeIdx < 0 ? n : closeIdx + closeTag.Length;
        }

        private static int IndexOfIgnoreCase(string haystack, string needle, int startIndex)
        {
            return haystack.IndexOf(needle, startIndex, StringComparison.OrdinalIgnoreCase);
        }

        private static int FindBalancedEnd(string input, int start, string open, string close)
        {
            int n = input.Length;
            int depth = 0;
            int i = start;

            while (i < n)
            {
                if (Match(input, i, open))
                {
                    depth++;
                    i += open.Length;
                    continue;
                }
                if (Match(input, i, close))
                {
                    depth--;
                    i += close.Length;
                    if (depth == 0) return i;
                    continue;
                }
                i++;
            }

            return n;
        }
    }

    public static class RuleSet
    {
        public static IReadOnlyList<AccentRule> Default { get; } = BuildDefault();

        private static IReadOnlyList<AccentRule> BuildDefault()
        {
            var pairs = new (string wrong, string correct)[]
            {
                ("a partir de",       "à partir de"),
                ("a partir du",       "à partir du"),
                ("a partir des",      "à partir des"),
                ("a travers",         "à travers"),
                ("a travers le",      "à travers le"),
                ("a travers la",      "à travers la"),
                ("a travers les",     "à travers les"),
                ("a cause de",        "à cause de"),
                ("a propos de",       "à propos de"),
                ("a l'aide de",       "à l'aide de"),
                ("a l'origine de",    "à l'origine de"),
                ("a l'origine",       "à l'origine"),
                ("a l'egard de",      "à l'égard de"),
                ("a l'égard de",      "à l'égard de"),
                ("a l'encontre de",   "à l'encontre de"),
                ("a coté de",         "à côté de"),
                ("a côté de",         "à côté de"),
                ("a dessein",         "à dessein"),
                ("a defaut de",       "à défaut de"),
                ("a défaut de",       "à défaut de"),
                ("a destination de",  "à destination de"),
                ("a domicile",        "à domicile"),
                ("a distance",        "à distance"),
                ("a droite",          "à droite"),
                ("a gauche",          "à gauche"),
                ("a nouveau",         "à nouveau"),
                ("a long terme",      "à long terme"),
                ("a court terme",     "à court terme"),
                ("a moyen terme",     "à moyen terme"),
                ("a peu pres",        "à peu près"),
                ("a peu près",        "à peu près"),
            };

            var rules = new List<AccentRule>(pairs.Length);
            foreach (var (wrong, correct) in pairs)
            {
                rules.Add(new AccentRule(wrong, correct));
            }
            return rules;
        }
    }

    public sealed class AAccentFixEngine
    {
        private readonly TextMasker _masker;
        private readonly IReadOnlyList<AccentRule> _rules;

        public AAccentFixEngine() : this(new TextMasker(), RuleSet.Default) { }

        public AAccentFixEngine(TextMasker masker, IReadOnlyList<AccentRule> rules)
        {
            _masker = masker;
            _rules = rules;
        }

        public FixResult Fix(string articleText)
        {
            var maskResult = _masker.Mask(articleText);
            string working = maskResult.MaskedText;

            int totalReplacements = 0;
            var appliedRuleNames = new List<string>();

            foreach (var rule in _rules)
            {
                int countForThisRule = 0;
                working = rule.Pattern.Replace(working, m =>
                {
                    countForThisRule++;
                    return rule.Apply(m);
                });

                if (countForThisRule > 0)
                {
                    totalReplacements += countForThisRule;
                    appliedRuleNames.Add($"{rule.WrongPhrase} -> {rule.CorrectPhrase} ({countForThisRule}x)");
                }
            }

            string finalText = _masker.Unmask(working, maskResult.ProtectedSpans);
            return new FixResult(finalText, totalReplacements, appliedRuleNames);
        }

        public static string BuildSummary(FixResult result)
        {
            if (result.ReplacementCount == 0) return string.Empty;

            var sb = new StringBuilder();
            sb.Append("Correction typographique : « a » → « à » (");
            sb.Append(result.ReplacementCount);
            sb.Append(result.ReplacementCount > 1 ? " occurrences)" : " occurrence)");
            return sb.ToString();
        }
    }

    public class AAccentFixPlugin : IAWBPlugin
    {
        private readonly AAccentFixEngine _engine = new AAccentFixEngine();

        public string Name => "AAccentFix";
        public string WikiName => string.Empty;

        public string ProcessArticle(IAutoWikiBrowser sender, IProcessArticleEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArticleText))
            {
                return e.ArticleText;
            }

            FixResult result = _engine.Fix(e.ArticleText);

            if (result.ReplacementCount == 0)
            {
                return e.ArticleText;
            }

            e.EditSummary = AAccentFixEngine.BuildSummary(result);
            return result.Text;
        }

        public void Initialise(IAutoWikiBrowser sender) { }
        public void LoadSettings(object[] prefs) { }
        public object[] SaveSettings() { return new object[0]; }
        public void Reset() { }
        public void Nudge(out bool nudged) { nudged = false; }
        public void Nudged(int nudges) { }
    }
}
