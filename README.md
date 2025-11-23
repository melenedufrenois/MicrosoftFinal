# 🎮 LoL Project — Nexus Dashboard

![Build Status](https://github.com/melenedufrenois/MicrosoftFinal/actions/workflows/DotnetCi.yml/badge.svg)

Bienvenue sur **LoL Project**, une application distribuée développée dans le cadre du projet **.NET / Aspire**.
Elle permet aux joueurs de *League of Legends* de consulter un wiki des champions, partager des astuces, et analyser leurs statistiques en temps réel via l'API officielle de Riot Games.

---

## 🚀 Technologies Utilisées

* **Orchestration :** .NET Aspire 9.0
* **Frontend :** Blazor Server (.NET 9) + Bootstrap (Thème Hextech)
* **Backend :** ASP.NET Core Minimal API
* **Base de données :** SQL Server (Entity Framework Core)
* **Authentification :** Keycloak (OpenID Connect)
* **Tests & CI :** xUnit, Moq, GitHub Actions

---

## ✨ Fonctionnalités

### 🔓 Publiques
* **Wiki Champions :** Liste complète synchronisée avec DataDragon.
* **Détails Champion :** Lore, statistiques de base (HP, AD, etc.) et astuces communautaires.

### 🔐 Utilisateurs Connectés
* **Dashboard Personnel :**
  * Liaison de compte Riot (EUW).
  * **Statistiques en direct :** Rang Solo/Duo, Winrate, KDA moyen, CS/minute.
  * **Historique des matchs :** Résumé des 20 dernières parties avec détails (Items, Sorts, Dégâts).
* **Conseil de Guerre :** Ajout de tips sur les champions.
* **Gestion :** Suppression de ses propres tips.

### 🛡️ Administration
* **Panel de Gestion Hextech :**
  * Synchronisation manuelle des données Riot (Champions, Versions).
  * Gestion des utilisateurs (Délier les comptes Riot).
  * Modération des Tips (Suppression/Edition).
  * Logs d'activité en temps réel.

---

## ⚙️ Configuration (Obligatoire)

Le projet utilise le **Secret Manager** de .NET pour ne pas exposer les clés API. Avant de lancer le projet, vous devez configurer vos secrets en local.

### 1. Clé API Riot
Obtenez une clé de développement sur [developer.riotgames.com](https://developer.riotgames.com/).

```bash
cd LoLProject.ApiService
dotnet user-secrets init
dotnet user-secrets set "RiotApi:ApiKey" "RGAPI-VOTRE-CLE-ICI"
