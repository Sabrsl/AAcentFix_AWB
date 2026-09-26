using System;
using System.Collections.Generic;
using System.Text;

namespace AAccentFix.Core
{
    /// <summary>
    /// Remplace temporairement les zones "protégées" d'un texte wiki (commentaires, nowiki,
    /// références, modèles/templates, liens internes, liens externes) par des jetons opaques,
    /// afin qu'aucune règle de correction ne puisse jamais les modifier.
    ///
    /// Approche volontairement conservatrice : en cas de doute (balise non refermée, imbrication
    /// suspecte), on protège large plutôt que de risquer une correction dans une zone sensible.
    /// </summary>
    public sealed class TextMasker
    {
        // Caractères de la zone Private Use Area d'Unicode : très improbable qu'ils apparaissent
        // dans un article Wikipédia normal, donc sûrs à utiliser comme délimiteurs de jetons.
        private const char TokenStart = '\uE000';
        private const char TokenEnd = '\uE001';

        public sealed class MaskResult
        {
            public string MaskedText { get; }
            public IReadOnlyList<string> ProtectedSpans { get; }

            public MaskResult(string maskedText, IReadOnlyList<string> protectedSpans)
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
                    int spanEnd = end < 0 ? n : end + 3; // si non fermé, protège jusqu'à la fin
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 2. <nowiki>...</nowiki> (et forme auto-fermante <nowiki/>)
                if (MatchTagOpen(input, i, "nowiki"))
                {
                    int spanEnd = FindTagEnd(input, i, "nowiki");
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 3. <ref ...>...</ref> ou <ref .../>
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

                // 5. Modèles {{ ... }} avec gestion de l'imbrication (Infobox, Portail, etc.)
                if (Match(input, i, "{{"))
                {
                    int spanEnd = FindBalancedEnd(input, i, "{{", "}}");
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 6. Liens internes [[ ... ]] avec gestion de l'imbrication
                //    (couvre au passage les catégories [[Catégorie:...]] et fichiers [[Fichier:...]])
                if (Match(input, i, "[["))
                {
                    int spanEnd = FindBalancedEnd(input, i, "[[", "]]");
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 7. Liens externes [http://... texte] ou [https://... texte]
                if (input[i] == '[' && (Match(input, i + 1, "http://") || Match(input, i + 1, "https://")))
                {
                    int end = input.IndexOf(']', i);
                    int spanEnd = end < 0 ? n : end + 1;
                    Emit(output, protectedSpans, input, i, spanEnd);
                    i = spanEnd;
                    continue;
                }

                // 8. Gras+italique '''''...''''' (protégé)
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

                // 9. Italique ''...'' (protégé) - avec lookaround pour éviter de mordre sur gras
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

                // 10. Guillemets français « ... » (protégés)
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

        public string Unmask(string maskedText, IReadOnlyList<string> protectedSpans)
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
                        // Jeton malformé : on recopie tel quel plutôt que de planter.
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
                        // Ne devrait jamais arriver ; sécurité en cas d'incohérence.
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

        private static bool MatchChar(string input, int pos, char c)
        {
            if (pos < 0 || pos >= input.Length) return false;
            return input[pos] == c;
        }

        private static bool NotMatchChar(string input, int pos, char c)
        {
            if (pos < 0 || pos >= input.Length) return true; // hors limites = pas de match
            return input[pos] != c;
        }

        private static bool MatchTagOpen(string input, int pos, string tagName)
        {
            // Reconnaît "<tagName" suivi d'un espace, '>' ou '/' (insensible à la casse).
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

        /// <summary>
        /// Trouve la fin d'un élément balisé de type &lt;tag ...&gt;...&lt;/tag&gt; ou &lt;tag .../&gt;.
        /// Si la balise n'est jamais refermée, protège jusqu'à la fin du texte (sécurité par défaut).
        /// </summary>
        private static int FindTagEnd(string input, int start, string tagName)
        {
            int n = input.Length;
            int openTagClose = input.IndexOf('>', start);
            if (openTagClose < 0) return n;

            // Forme auto-fermante : <tag .../>
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

        /// <summary>
        /// Trouve la fin d'une construction délimitée par des paires ouvrantes/fermantes qui
        /// peuvent s'imbriquer (ex : {{ }} pour les modèles, [[ ]] pour les liens).
        /// Si jamais refermée correctement, protège jusqu'à la fin du texte.
        /// </summary>
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

            return n; // non refermé : on protège jusqu'à la fin par sécurité
        }
    }
}
