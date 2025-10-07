# Chatbot d'Entretien d'Embauche

Un chatbot intelligent conçu pour aider les personnes vulnérables à se préparer aux entretiens d'embauche, en utilisant l'API GPT-4o pour simuler des entretiens et fournir des feedbacks personnalisés.

## Fonctionnalités

- **Simulation d'entretiens d'embauche** adaptée au profil et au secteur recherché
- **Approche par étapes** : introduction, préparation, simulation, feedback
- **Interface accessible** pour les personnes avec un faible niveau de compétences numériques
- **Base de connaissances** intégrée sur les bonnes pratiques d'entretien d'embauche
- **Feedback constructif et bienveillant** après chaque simulation

## Architecture

Le projet est structuré comme suit:

- **Services/** : Contient les services d'intégration avec OpenAI et le service de dialogue
- **Models/** : Définit les modèles de données utilisés dans l'application
- **Controllers/** : Points d'entrée API pour l'application
- **Guide_Embauche.markdown** : Base de connaissances utilisée pour guider les réponses de l'IA

## Configuration

1. **Clé API OpenAI** : Vous devez configurer une clé API OpenAI valide dans `appsettings.json` ou via une variable d'environnement `OPENAI_API_KEY`.

2. **Paramètres de l'application** : Configurez les paramètres de l'application dans `appsettings.json`.

## Démarrage rapide

1. Clonez ce dépôt
2. Configurez votre clé API OpenAI dans `appsettings.json`
3. Exécutez l'application avec `dotnet run`
4. Accédez à l'interface via un navigateur à l'adresse http://localhost:3978

## Flux de conversation

Le chatbot suit un flux de conversation structuré:

- 📋 **Préparation à l'entretien**: conseils sur la recherche d'entreprise, questions fréquentes, etc.
- 💬 **Simulation d'entretien**: pratique interactive avec des questions réalistes
- 📊 **Analyse des réponses**: retours constructifs pour s'améliorer
- ❓ **Conseils généraux**: bonnes pratiques pour l'entretien
- ⚙️ **Personnalisation**: adaptation selon le profil et les besoins spécifiques

## Extensibilité

Le système est conçu pour être facilement extensible:

- Ajout de nouvelles questions d'entretien
- Intégration de nouveaux secteurs professionnels
- Personnalisation des prompts système
- Extension pour d'autres langues

## Contribuer

Les contributions sont les bienvenues ! N'hésitez pas à proposer des améliorations via des pull requests ou à signaler des problèmes.
