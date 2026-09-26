using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AAccentFix.Core
{
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

    /// <summary>
    /// Point d'entrée unique du plugin : à appeler avec le wikitexte brut d'un article.
    /// Ne modifie jamais le contenu des zones protégées (voir TextMasker).
    /// </summary>
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

        /// <summary>
        /// Construit un résumé de modification au format attendu par AWB / les résumés wiki.
        /// </summary>
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
}
