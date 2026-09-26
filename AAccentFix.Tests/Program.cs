using System;
using System.Collections.Generic;
using AAccentFix.Core;

namespace AAccentFix.Tests
{
    /// <summary>
    /// Suite de tests volontairement sans framework de test externe (pas de NuGet requis),
    /// pour pouvoir compiler et exécuter avec seulement le SDK .NET installé localement :
    ///
    ///   dotnet run --project AAccentFix.Tests
    ///
    /// Code de sortie 0 si tout passe, 1 sinon (utilisable en CI).
    /// </summary>
    internal static class Program
    {
        private static int _passed = 0;
        private static int _failed = 0;

        private static int Main()
        {
            var engine = new AAccentFixEngine();

            // --- Cas qui DOIVENT être corrigés ---------------------------------------

            AssertFixed(engine, "L'entreprise a partir de 2020 va croître.",
                "L'entreprise à partir de 2020 va croître.");

            AssertFixed(engine, "A partir de demain, tout change.",
                "À partir de demain, tout change.");

            AssertFixed(engine, "Il a regardé a travers la fenêtre.",
                "Il a regardé à travers la fenêtre.");

            AssertFixed(engine, "Ceci se trouve a cote de la gare.",
                "Ceci se trouve a cote de la gare."); // "cote" sans accent n'est pas dans la liste, doit rester inchangé
            // ^ Ce test documente une limite volontaire : "a cote de" (sans accent sur cote)
            //   n'est PAS dans la liste blanche (seuls "a coté de" et "a côté de" le sont),
            //   pour éviter d'empiler deux corrections différentes dans une seule règle.

            AssertFixed(engine, "Il vit a coté de chez moi.",
                "Il vit à côté de chez moi.");

            AssertFixed(engine, "On l'utilise a l'aide de ciseaux.",
                "On l'utilise à l'aide de ciseaux.");

            // Apostrophe typographique en entrée : la règle la reconnaît, mais la correction
            // produite utilise toujours l'apostrophe droite standard (comportement voulu,
            // cohérent quel que soit le style d'apostrophe rencontré dans l'article).
            AssertFixed(engine, "On l'utilise a l’aide de ciseaux.",
                "On l'utilise à l'aide de ciseaux.");

            AssertFixed(engine, "Objectif a long terme et a court terme.",
                "Objectif à long terme et à court terme.");

            // --- Cas qui NE DOIVENT JAMAIS être touchés -------------------------------

            AssertUnchanged(engine, "Il a publié un article en 2020.");
            AssertUnchanged(engine, "On a constaté une hausse.");
            AssertUnchanged(engine, "La société a annoncé un partenariat.");
            AssertUnchanged(engine, "Il a été nommé directeur.");
            AssertUnchanged(engine, "Elle a fait ses preuves.");

            // --- Zones protégées : le contenu ne doit jamais être modifié -------------

            AssertUnchanged(engine,
                "Texte normal <ref>Livre publié a partir de 1990, Editions Dupont</ref> suite.");

            AssertUnchanged(engine,
                "Voir [[Categorie:A partir de 2020]] pour la liste.");

            AssertUnchanged(engine,
                "{{Infobox | date = a partir de 2020 }}");

            AssertUnchanged(engine,
                "[http://example.com/a-partir-de-2020 a partir de 2020]");

            AssertUnchanged(engine,
                "<!-- commentaire : a partir de maintenant, ignorer ce paragraphe -->");

            AssertUnchanged(engine,
                "<nowiki>a partir de</nowiki>");

            // --- Cas mixtes : correction dans le texte visible, protection dans la ref --

            {
                string input = "L'entreprise a partir de 2020 se développe<ref>Créée a partir de rien</ref>.";
                string expected = "L'entreprise à partir de 2020 se développe<ref>Créée a partir de rien</ref>.";
                AssertFixed(engine, input, expected);
            }

            // --- Modèle imbriqué : protège tout le bloc, même avec des {{ }} internes ---

            AssertUnchanged(engine,
                "{{Infobox société | filiale = {{Nobr|a partir de 2020}} }}");

            // --- Tests pour les modèles de citation et références ---

            AssertUnchanged(engine,
                "{{Citation bloc|Il a dit que a partir de demain tout change.}}");

            AssertUnchanged(engine,
                "{{Lien web |titre=Histoire a partir de 2020}}");

            AssertUnchanged(engine,
                "{{Article |titre=Évolution a travers les siècles}}");

            // --- Tests pour les protections de titres (liens wiki uniquement) ---

            AssertUnchanged(engine,
                "[[A côté de moi]]");

            AssertUnchanged(engine,
                "[[Album|A côté de moi]]");

            // --- Italique : protégé -----------------------------------------------
            AssertUnchanged(engine,
                "Le titre ''A côté de moi'' est populaire.");

            // --- Guillemets français : protégé --------------------------------------
            AssertUnchanged(engine,
                "« A côté de moi » est un titre.");

            // --- Gras+italique : protégé --------------------------------------------
            AssertUnchanged(engine,
                "Le titre '''''A côté de moi''''' est populaire.");

            // --- Italique suivi de texte normal avec apostrophe de contraction :
            //     vérifie que le lookaround ne fait pas déborder le match italique
            //     sur "c'est" (apostrophe simple, pas de run de 2+).
            {
                string input = "''Street'', c'est le titre a partir de 2020.";
                string expected = "''Street'', c'est le titre à partir de 2020.";
                AssertFixed(engine, input, expected);
            }

            // --- Plusieurs italiques distincts sur la même ligne --------------------
            AssertUnchanged(engine,
                "''Street'' puis ''A côté de moi'' sortent la même année.");

            // --- Test pour vérifier que le gras n'est PAS protégé (comme demandé) ---

            {
                string input = "Le titre '''A côté de moi''' est populaire.";
                string expected = "Le titre '''À côté de moi''' est populaire.";
                AssertFixed(engine, input, expected);
            }

            // --- Test mixte : texte normal corrigé, références protégées ---

            {
                string input = "Le village a partir de 2020 s'est développé<ref>{{Lien web |titre=Ville a partir de 2020}}</ref>.";
                string expected = "Le village à partir de 2020 s'est développé<ref>{{Lien web |titre=Ville a partir de 2020}}</ref>.";
                AssertFixed(engine, input, expected);
            }

            Console.WriteLine();
            Console.WriteLine($"Résultat : {_passed} test(s) réussi(s), {_failed} échec(s).");
            return _failed == 0 ? 0 : 1;
        }

        private static void AssertFixed(AAccentFixEngine engine, string input, string expected)
        {
            var result = engine.Fix(input);
            Check(result.Text == expected,
                $"CORRECTION ATTENDUE\n  entrée   : {input}\n  attendu  : {expected}\n  obtenu   : {result.Text}");
        }

        private static void AssertUnchanged(AAccentFixEngine engine, string input)
        {
            var result = engine.Fix(input);
            Check(result.Text == input,
                $"NE DEVAIT PAS CHANGER\n  entrée   : {input}\n  obtenu   : {result.Text}");
        }

        private static void Check(bool condition, string failureMessage)
        {
            if (condition)
            {
                _passed++;
            }
            else
            {
                _failed++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ÉCHEC : " + failureMessage);
                Console.ResetColor();
            }
        }
    }
}
