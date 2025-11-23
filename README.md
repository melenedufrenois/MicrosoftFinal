# 🎮 LoL Project — Nexus Dashboard

Application web distribuée développée dans le cadre du projet semestriel **.NET / Aspire** (M2 CYBER).
L'application permet de consulter un Wiki League of Legends, de gérer des astuces communautaires et d'analyser un compte joueur en temps réel via l'API Riot Games.

-----

## 📋 Prérequis Techniques

Pour lancer l'orchestrateur et les conteneurs, vous avez besoin de :

1.  **Docker Desktop** (installé et lancé).
2.  **.NET 9.0 SDK**.
3.  Une clé API Riot (Development Key) à récupérer sur [developer.riotgames.com](https://developer.riotgames.com/).

-----

## ⚙️ Configuration (Obligatoire)

Le projet respecte les bonnes pratiques de sécurité ("Clean Code") et ne stocke aucun secret dans le code source.
**Avant de lancer l'application**, vous devez configurer les secrets utilisateurs en local via le terminal :

### 1\. Configurer l'API (Clé Riot)

```bash
cd LoLProject.ApiService
dotnet user-secrets init
dotnet user-secrets set "RiotApi:ApiKey" "VOTRE-CLE-RIOT-ICI"
```

### 2\. Configurer le Frontend (Secret Keycloak)

*Le ClientSecret par défaut est configuré dans Keycloak, mais pour simuler une config propre :*

```bash
cd ../LoLProject.WebApp
dotnet user-secrets init
dotnet user-secrets set "Authentication:OIDC:ClientSecret" "SwtGRcBEIBs5F9OoJI9Em544BOB5uI5p"
```

*(Note : Retournez à la racine du projet après ces commandes : `cd ..`)*

-----

## 🚀 Lancement de l'Application

Le projet utilise **.NET Aspire** pour orchestrer l'API, le Frontend, la Base de données (SQL Server) et l'Authentification (Keycloak).

1.  Placez-vous à la racine du projet.
2.  Lancez l'hôte d'application :

<!-- end list -->

```bash
dotnet run --project LoLProject.AppHost/LoLProject.AppHost.csproj
```

3.  Une URL va s'afficher dans la console (ex: `http://localhost:15063`). Cliquez dessus pour ouvrir le **Dashboard Aspire**.
4.  Depuis ce dashboard, vous pourrez accéder à tous les services :
      * **webapp** : Le site principal (Frontend Blazor).
      * **apiservice** : L'API Backend (Swagger).
      * **keycloak** : La console d'administration (User: `admin` / Pass: `admin`).
      * **sql** : Le serveur de base de données.

-----

## ✨ Fonctionnalités & Contraintes Respectées

### 1\. Authentification & Rôles (Keycloak)

  * **Serveur OIDC :** Keycloak tourne dans un conteneur géré par Aspire.
  * **Rôle Utilisateur :** Accès au Dashboard personnel et ajout de Tips.
  * **Rôle Admin (Gestionnaire) :** Accès au panneau d'administration (bouton visible dans le menu latéral).

### 2\. Pages & Accès

  * 🟢 **Publique :** Page d'accueil, Liste des Champions (Wiki).
  * 🟢 **Publique :** Détail d'un champion (Consultation des stats/lore).
  * 🟠 **Authentifié :** Mon Dashboard (Liaison API Riot, Historique, Rang, CS/Min).
  * 🟠 **Authentifié :** Ajout et suppression de ses propres "Tips" (Conseils).
  * 🔴 **Admin :** Page `/admin` pour synchroniser les données Riot, purger la BDD et gérer les utilisateurs.

### 3\. Données & Architecture

  * **Base de données :** SQL Server via Entity Framework Core.
  * **Relations :** \* `AppUser` 1-1 `Summoner`
      * `Champion` 1-n `ChampionTip`
      * `Champion` 1-1 `ChampionStat`
  * **Architecture :** Séparation stricte Frontend (Blazor) / Backend (Minimal API) / Persistence / ServiceDefaults.
  * **CI/CD :** Workflow GitHub Actions exécutant les tests unitaires et d'intégration à chaque push.

-----

## 🧪 Tests

Le projet contient une suite de tests (xUnit) couvrant :

  * Les endpoints publics (Champions).
  * Les endpoints protégés (Dashboard, User Sync).
  * La logique métier (Calcul KDA, Winrate).
  * La sécurité (Impossibilité de supprimer le contenu d'autrui).

Pour lancer les tests (qui utilisent une base de données en mémoire isolée) :

```bash
dotnet test
```

-----

## 👥 Auteurs

Projet réalisé par **Mehdi TRARI** & **Mélène DUFRENOIS**.
