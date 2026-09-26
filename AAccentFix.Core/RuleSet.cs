using System.Collections.Generic;

namespace AAccentFix.Core
{
    /// <summary>
    /// Liste blanche des locutions "a X" -> "à X" à très forte confiance.
    ///
    /// Méthode de sélection : pour CHAQUE candidate, on se demande "est-ce que
    /// 'avoir' + [cette même suite de mots] forme une phrase française plausible ?"
    /// Si oui (même rarement), la locution est écartée et documentée dans
    /// <see cref="ExcludedCandidates"/> plutôt que d'être ajoutée ici.
    ///
    /// Exemple du piège type : "à l'occasion de" (préposition, "en l'honneur de X")
    /// vs "avoir l'occasion de" (verbe, "avoir la possibilité de faire X") -
    /// les deux commencent par les 4 mêmes mots "a l'occasion de" et ne se
    /// distinguent QUE par ce qui suit (un nom vs un infinitif), que ce plugin
    /// ne regarde jamais. Une telle locution est donc TOUJOURS exclue, même si
    /// elle est très fréquente - c'est justement parce qu'elle est fréquente
    /// des deux côtés qu'elle est dangereuse.
    /// </summary>
    public static class RuleSet
    {
        public static IReadOnlyList<AccentRule> Default { get; } = BuildDefault();

        /// <summary>
        /// Candidates étudiées puis délibérément écartées de <see cref="Default"/>,
        /// conservées ici uniquement à des fins de documentation/audit (jamais
        /// utilisées par le moteur). Sert à éviter qu'une de ces locutions soit
        /// rajoutée plus tard sans relire pourquoi elle a été écartée.
        /// </summary>
        public static IReadOnlyList<(string phrase, string reason)> ExcludedCandidates { get; } = BuildExcluded();

        private static IReadOnlyList<AccentRule> BuildDefault()
        {
            var pairs = new List<(string wrong, string correct)>();

            // --- Constructions temporelles / structurantes déjà en place ------------
            AddRange(pairs, new (string, string)[]
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
                ("a l'aide de",       "à l'aide de"), // risque résiduel faible, voir note plus bas
                ("a l'origine de",    "à l'origine de"),
                ("a l'origine",       "à l'origine"),
                ("a l'egard de",      "à l'égard de"),
                ("a l'égard de",      "à l'égard de"),
                ("a l'encontre de",   "à l'encontre de"),
                ("a coté de",         "à côté de"),
                ("a côté de",         "à côté de"),
                ("a defaut de",       "à défaut de"),
                ("a défaut de",       "à défaut de"),
                ("a defaut",          "à défaut"),
                ("a défaut",          "à défaut"),
                ("a destination de",  "à destination de"),
                ("a domicile",        "à domicile"), // risque résiduel faible (juridique : "avoir domicile à"), gardé car très fréquent (sport, livraison)
                ("a distance",        "à distance"),
                ("a droite",          "à droite"),
                ("a gauche",          "à gauche"),
                ("a nouveau",         "à nouveau"),
                ("a long terme",      "à long terme"),
                ("a court terme",     "à court terme"),
                ("a moyen terme",     "à moyen terme"),
                ("a peu pres",        "à peu près"),
                ("a peu près",        "à peu près"),
            });

            // --- Lieu / géographie (très fréquent dans les articles Wikipédia) ------
            AddRange(pairs, new (string, string)[]
            {
                ("a l'est de",          "à l'est de"),
                ("a l'ouest de",        "à l'ouest de"),
                ("a l'interieur de",    "à l'intérieur de"),
                ("a l'intérieur de",    "à l'intérieur de"),
                ("a l'interieur",       "à l'intérieur"),
                ("a l'intérieur",       "à l'intérieur"),
                ("a l'exterieur de",    "à l'extérieur de"),
                ("a l'extérieur de",    "à l'extérieur de"),
                ("a l'exterieur",       "à l'extérieur"), // fréquent en contexte sportif : "match joué a l'exterieur"
                ("a l'extérieur",       "à l'extérieur"),
                ("a proximite de",      "à proximité de"),
                ("a proximité de",      "à proximité de"),
                ("a l'entree de",       "à l'entrée de"),
                ("a l'entrée de",       "à l'entrée de"),
                ("a l'arriere de",      "à l'arrière de"),
                ("a l'arrière de",      "à l'arrière de"),
                ("a l'avant de",        "à l'avant de"),
                ("a l'ecart de",        "à l'écart de"),
                ("a l'écart de",        "à l'écart de"),
                ("a l'echelle de",      "à l'échelle de"),
                ("a l'échelle de",      "à l'échelle de"),
                ("a l'echelle",         "à l'échelle"),
                ("a l'échelle",         "à l'échelle"),
                ("a l'etranger",        "à l'étranger"),
                ("a l'étranger",        "à l'étranger"),
            });

            // --- Temporel --------------------------------------------------------------
            AddRange(pairs, new (string, string)[]
            {
                ("a l'epoque",       "à l'époque"),
                ("a l'époque",       "à l'époque"),
                ("a l'epoque de",    "à l'époque de"),
                ("a l'époque de",    "à l'époque de"),
                ("a cette epoque",   "à cette époque"),
                ("a cette époque",   "à cette époque"),
                ("a l'heure actuelle", "à l'heure actuelle"),
                ("a ce jour",         "à ce jour"),
                ("a ce moment-la",    "à ce moment-là"),
                ("a ce moment-là",    "à ce moment-là"),
                ("a l'aube de",       "à l'aube de"),
                ("a la veille de",    "à la veille de"),
                ("a l'issue de",      "à l'issue de"), // risque résiduel très faible, voir note
            });

            // --- Comparaison / mise en perspective --------------------------------------
            AddRange(pairs, new (string, string)[]
            {
                ("a l'instar de",      "à l'instar de"),
                ("a la difference de", "à la différence de"),
                ("a la différence de", "à la différence de"),
                ("a la maniere de",    "à la manière de"),
                ("a la manière de",    "à la manière de"),
                ("a l'exception de",   "à l'exception de"),
                ("a l'exception des",  "à l'exception des"),
                ("a l'exclusion de",   "à l'exclusion de"),
                ("a l'inverse",        "à l'inverse"),
                ("a l'inverse de",     "à l'inverse de"),
                ("a l'exemple de",     "à l'exemple de"),
            });

            // --- Discursif / logique -----------------------------------------------------
            AddRange(pairs, new (string, string)[]
            {
                ("a ce titre",           "à ce titre"),
                ("a cet egard",          "à cet égard"),
                ("a cet égard",          "à cet égard"),
                ("a ce sujet",           "à ce sujet"),
                ("a cet effet",          "à cet effet"),
                ("a savoir",             "à savoir"),
                ("a plus forte raison",  "à plus forte raison"),
            });

            // --- Quantité / mesure ---------------------------------------------------------
            AddRange(pairs, new (string, string)[]
            {
                ("a hauteur de",      "à hauteur de"),
                ("a concurrence de",  "à concurrence de"),
                ("a peu de frais",    "à peu de frais"),
                ("a moitie",          "à moitié"),
                ("a moitié",          "à moitié"),
            });

            // --- Locutions adverbiales figées (quasi jamais objets d'un verbe) -------------
            AddRange(pairs, new (string, string)[]
            {
                ("a bras le corps",   "à bras le corps"),
                ("a bras ouverts",    "à bras ouverts"),
                ("a contre-courant",  "à contre-courant"),
                ("a contrecoeur",     "à contrecœur"),
                ("a contrecœur",      "à contrecœur"),
                ("a demi",            "à demi"),
                ("a foison",          "à foison"),
                ("a huis clos",       "à huis clos"),
                ("a jamais",          "à jamais"),
                ("a la hate",         "à la hâte"),
                ("a la hâte",         "à la hâte"),
                ("a loisir",          "à loisir"),
                ("a merveille",       "à merveille"),
                ("a outrance",        "à outrance"),
                ("a profusion",       "à profusion"),
                ("a tatons",          "à tâtons"),
                ("a tâtons",          "à tâtons"),
                ("a tue-tete",        "à tue-tête"),
                ("a tue-tête",        "à tue-tête"),
                ("a vrai dire",       "à vrai dire"),
                ("a volonte",         "à volonté"),
                ("a volonté",         "à volonté"),
                ("a vue d'oeil",      "à vue d'œil"),
                ("a vue d'œil",       "à vue d'œil"),
                ("a bout portant",    "à bout portant"),
                ("a corps perdu",     "à corps perdu"),
                ("a cor et a cri",    "à cor et à cri"),
                ("a double tranchant","à double tranchant"),
                ("a froid",           "à froid"),
                ("a l'aveuglette",    "à l'aveuglette"),
                ("a l'amiable",       "à l'amiable"),
                ("a l'improviste",    "à l'improviste"),
                ("a l'unanimite",     "à l'unanimité"),
                ("a l'unanimité",     "à l'unanimité"),
                ("a la chaine",       "à la chaîne"),
                ("a la chaîne",       "à la chaîne"),
                ("a la derobee",      "à la dérobée"),
                ("a la dérobée",      "à la dérobée"),
                ("a la ronde",        "à la ronde"),
                ("a la sauvette",     "à la sauvette"),
                ("a mi-chemin",       "à mi-chemin"),
                ("a mi-voix",         "à mi-voix"),
                ("a perte de vue",    "à perte de vue"),
                ("a point nomme",     "à point nommé"),
                ("a point nommé",     "à point nommé"),
                ("a reculons",        "à reculons"),
                ("a tout hasard",     "à tout hasard"),
                ("a tout prix",       "à tout prix"),
                ("a tout jamais",     "à tout jamais"),
                ("a voix haute",      "à voix haute"),
                ("a voix basse",      "à voix basse"),
                ("a demi-mot",        "à demi-mot"),
            });

            var rules = new List<AccentRule>(pairs.Count);
            foreach (var (wrong, correct) in pairs)
            {
                rules.Add(new AccentRule(wrong, correct));
            }
            return rules;
        }

        private static IReadOnlyList<(string phrase, string reason)> BuildExcluded()
        {
            return new (string phrase, string reason)[]
            {
                ("a l'occasion de",
                 "Collision directe avec « avoir l'occasion de + infinitif » (« il a l'occasion de partir »), " +
                 "extrêmement fréquent. Les deux constructions partagent les 4 mêmes premiers mots et ne se " +
                 "distinguent que par ce qui suit (nom vs infinitif), que le plugin ne regarde pas."),

                ("a l'intention de",
                 "Collision directe avec « avoir l'intention de + infinitif » (« il a l'intention de partir »), " +
                 "construction extrêmement courante. Même piège que « à l'occasion de »."),

                ("a l'appui de",
                 "Collision avec « avoir l'appui de quelqu'un » (« il a l'appui de ses collègues », " +
                 "= bénéficier du soutien de). Usage réel et pas rare."),

                ("a l'ecoute de",
                 "Collision avec l'expression « avoir l'écoute de quelqu'un » (« il a l'écoute du ministre », " +
                 "= avoir son attention/audience). Usage réel, notamment en contexte politique/journalistique."),

                ("a la place de",
                 "Collision avec « avoir la place de + infinitif » (« il a la place de se garer », " +
                 "= disposer de suffisamment d'espace pour). Usage réel, notamment automobile/immobilier."),

                ("a la suite de",
                 "Collision avec « avoir la suite de quelque chose » (« il a la suite de l'histoire », " +
                 "= posséder/connaître la suite). Moins fréquent que les autres cas mais bien réel."),

                ("a la tete de",
                 "Collision avec l'expression figée « avoir la tête de l'emploi » et, plus largement, " +
                 "avec « avoir la tête de quelqu'un » au sens propre (portraits, ressemblance)."),

                ("a raison de",
                 "Collision directe et fréquente avec « avoir raison de » (au sens de « avoir raison » : " +
                 "« il a raison de partir » = il a de bonnes raisons de partir ; ou au sens de " +
                 "« vaincre » : « avoir raison de son adversaire »). Ne jamais ajouter cette locution."),

                ("a tort",
                 "Collision garantie avec « avoir tort » (« il a tort », extrêmement fréquent, souvent en fin " +
                 "de phrase sans rien après). Corriger systématiquement introduirait des erreurs massives."),

                ("a peine",
                 "Collision directe avec « avoir peine à + infinitif » (« il a peine à croire », = avoir du mal " +
                 "à). Les deux graphies sont grammaticalement correctes selon le sens voulu ; impossible à " +
                 "trancher sans analyse syntaxique complète."),

                ("a dessein",
                 "Collision avec la construction littéraire/archaïque « avoir dessein de + infinitif » " +
                 "(« il a dessein de partir » = il a l'intention de). Rare dans le français contemporain " +
                 "mais peut apparaître dans des articles sur des textes anciens/historiques."),

                ("a l'avenir",
                 "Collision avec l'expression figée « avoir l'avenir devant soi » (« il a l'avenir devant lui »)." +
                 " « À l'avenir » (adverbe, = dorénavant) et cette expression partagent le même préfixe " +
                 "« a l'avenir »."),

                ("a l'initiative de",
                 "Collision avec « avoir l'initiative de/du + nom » en contexte sportif/militaire " +
                 "(« l'équipe qui a l'initiative du jeu »). Le sens encyclopédique le plus courant " +
                 "(« lancé à l'initiative de X ») est correct, mais le risque n'est pas négligeable."),

                ("a contrario",
                 "PIÈGE : locution latine, s'écrit SANS accent en français (« a contrario », comme « a priori », " +
                 "« a fortiori », « a posteriori »). Ne jamais « corriger » - ce serait introduire une faute."),

                ("a priori",
                 "PIÈGE : locution latine invariable, s'écrit SANS accent. Ne jamais corriger."),

                ("a posteriori",
                 "PIÈGE : locution latine invariable, s'écrit SANS accent. Ne jamais corriger."),

                ("a fortiori",
                 "PIÈGE : locution latine invariable, s'écrit SANS accent. Ne jamais corriger."),
            };
        }

        private static void AddRange(List<(string wrong, string correct)> target, (string, string)[] items)
        {
            target.AddRange(items);
        }
    }
}
