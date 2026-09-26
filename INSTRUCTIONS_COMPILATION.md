# Instructions de compilation

## Prérequis

- .NET Framework 4.8 ou 4.8.1 Developer Pack
- AutoWikiBrowser installé
- Visual Studio 2019 ou ultérieur

## Compilation du plugin AWB

### 1. Ouvrir le projet

Ouvrir `AAccentFix.sln` dans Visual Studio.

### 2. Ajouter la référence WikiFunctions

1. Clic droit sur le projet `AAccentFix.Awb` dans l'Explorateur de solutions
2. Ajouter > Référence > Parcourir
3. Sélectionner `WikiFunctions.dll` depuis le dossier d'installation AWB
4. Valider

### 3. Compiler

1. Passer en configuration Release
2. Générer la solution (Ctrl+Shift+B)

### 4. Déploiement

1. Copier `AAccentFix.Awb.dll` depuis `AAccentFix.Awb\bin\Release\net481\`
2. Coller dans le dossier `Plugins` de l'installation AWB

## Compilation des tests

```bash
dotnet test AAccentFix.Tests
```

Résultat attendu : 33 tests réussis

## Résolution de problèmes

**Type 'IAWBPlugin' introuvable**
- Vérifier que la référence à WikiFunctions.dll est correctement ajoutée
- Redémarrer Visual Studio après l'ajout de la référence

**Erreur de compilation .NET Framework**
- Installer le .NET Framework 4.8 Developer Pack
- Redémarrer Visual Studio après l'installation
