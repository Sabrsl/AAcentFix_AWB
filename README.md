# AAccentFix — Plugin AWB pour la correction typographique a → à

Plugin AutoWikiBrowser corrigeant automatiquement les fautes d'accent sur la préposition "a" (à).

## Fonctionnalités

Correction des locutions suivantes :

`a` → `à` dans les expressions :

- `a` partir de/du/des
- `a` travers (le/la/les)
- `a` cause de
- `a` propos de
- `a` l'aide de
- `a` l'origine de
- `a` l'egard de
- `a` l'encontre de
- `a` cote de
- `a` dessein
- `a` defaut de
- `a` destination de
- `a` domicile
- `a` distance
- `a` droite
- `a` gauche
- `a` nouveau
- `a` long/court/moyen terme
- `a` peu pres

## Zones protégées

Le plugin ne modifie pas le texte dans :
- Références `<ref>...</ref>`
- Commentaires HTML `<!-- ... -->`
- Balises `<nowiki>...</nowiki>`
- Modèles `{{...}}` (incluant citations, fichiers, liens, etc.)
- Liens internes `[[...]]`
- Liens externes `[http://... ...]`
- Italique `''...''`
- Gras+italique `'''''...'''''`
- Guillemets français « ... »

## Limitations

Le gras seul n'est pas protégé :
- `'''A côté de moi'''` sera modifié
- Le texte dans les tableaux n'est pas protégé

## Tests

33 tests unitaires validés couvrant :
- Corrections attendues avec apostrophe typographique et majuscules
- Cas à ne pas modifier (auxiliaires, locutions intentionnelles)
- Protection des zones structurées
- Mise en forme inline

Exécution : `dotnet test AAccentFix.Tests`

## Structure du projet

```
AAccentFix.sln
AAccentFix.Core/     — Moteur de correction (net8.0 pour tests)
AAccentFix.Awb/      — Wrapper plugin AWB (net481)
AAccentFix.Tests/    — Tests unitaires
NuGet.Config         — Configuration NuGet hors-ligne
```

## Compilation

Prérequis :
- .NET Framework 4.8 ou 4.8.1 Developer Pack
- AutoWikiBrowser installé
- Visual Studio 2019+

### Tests (sans dépendance AWB)

```bash
dotnet test AAccentFix.Tests
```

### Plugin AWB

1. Ouvrir `AAccentFix.sln` dans Visual Studio
2. Ajouter une référence à `WikiFunctions.dll` depuis l'installation AWB
   - Clic droit sur AAccentFix.Awb > Ajouter > Référence > Parcourir
3. Compiler en Release
4. Copier `AAccentFix.Awb.dll` vers le dossier Plugins d'AWB

## Déploiement

Procédure recommandée :
1. Tester sur 10 pages maximum
2. Vérifier chaque diff manuellement
3. Élargir progressivement
   
## Personnalisation

Les règles de correction sont définies dans `AAccentFix.Core/RuleSet.cs`.

Pour ajouter une règle :
1. Ajouter la paire `(mauvais, correct)` dans RuleSet.cs
2. Ajouter un test correspondant dans Program.cs
3. Recompiler
