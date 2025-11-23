# 🎮 LoL Project — Nexus Dashboard

Application web distribuée développée dans le cadre du projet semestriel **.NET / Aspire** (M2 CYBER).
L'application permet de consulter un Wiki League of Legends, de gérer des astuces communautaires et d'analyser un compte joueur en temps réel via l'API Riot Games.

### 📺 Présentation du projet
**[Voir la vidéo de démonstration sur YouTube](https://youtu.be/0ujCTgUZrZI)**

-----

## ⚠️ Note sur la version en ligne & Clé API

Nous allons tenter de mettre une version du site en ligne pour faciliter la consultation.

**Cependant, une contrainte technique importante existe :**
Les clés API Riot (Development Key) expirent automatiquement au bout de **24 heures**. Par conséquent, même si le site est accessible en ligne, il est fort probable que la clé utilisée lors du déploiement soit expirée au moment de votre correction.
* **Si la clé est expirée :** Les appels API (Recherche de joueur, Dashboard) échoueront.
* **Recommandation :** Pour tester l'intégralité des fonctionnalités en temps réel, il est préférable de lancer le projet en **local** avec votre propre clé API fraîchement générée.

-----

## 📋 Prérequis Techniques

Pour lancer l'orchestrateur et les conteneurs en local, vous avez besoin de :

1.  **Docker Desktop** (installé et lancé).
2.  **.NET 9.0 SDK**.
3.  Une clé API Riot (Development Key) à récupérer sur [developer.riotgames.com](https://developer.riotgames.com/).

-----

## ⚙️ Configuration (Obligatoire)

Le projet respecte les bonnes pratiques de sécurité ("Clean Code") et ne stocke aucun secret dans le code source.
**Avant de lancer l'application**, vous devez configurer les secrets utilisateurs en local via le terminal :

### 1. Configurer l'API (Clé Riot)

```bash
cd LoLProject.ApiService
dotnet user-secrets init
dotnet user-secrets set "RiotApi:ApiKey" "VOTRE-CLE-RIOT-ICI"
