using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AAccentFix.Core
{
    /// <summary>
    /// Une règle de correction unique et à très forte confiance : une locution figée commençant
    /// par "a" qui doit commencer par "à" (ex : "a partir de" -> "à partir de").
    ///
    /// La règle ne s'applique QUE sur cette locution exacte (mots séparés par des espaces
    /// variables, apostrophes normalisées), jamais sur un "a" isolé.
    /// </summary>
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
            // Découpe la locution en mots, échappe chaque mot, autorise une apostrophe
            // typographique ou droite, et un nombre variable d'espaces entre les mots.
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

        /// <summary>
        /// Applique la règle sur un match trouvé, en préservant la casse de la première lettre
        /// (pour gérer "A partir de" en début de phrase -> "À partir de").
        /// </summary>
        public string Apply(Match match)
        {
            bool firstLetterUpper = match.Value.Length > 0 && char.IsUpper(match.Value[0]);
            if (!firstLetterUpper) return CorrectPhrase;

            return char.ToUpperInvariant(CorrectPhrase[0]) + CorrectPhrase.Substring(1);
        }
    }
}
