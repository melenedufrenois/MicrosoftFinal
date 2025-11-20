# 🎮 LoL Project — Application de Dashboard League of Legends

Bienvenue sur **LoL Project**, une application distribuée développée dans le cadre du projet **.NET / Aspire**.
Elle permet aux joueurs de *League of Legends* de consulter un wiki des champions, partager des astuces, et lier leur compte Riot pour consulter leurs statistiques.

---

## 🚀 Technologies Utilisées

* **Orchestration :** .NET Aspire 9.0
* **Frontend :** Blazor Server (.NET 9) + Bootstrap
* **Backend :** ASP.NET Core Minimal API
* **Base de données :** SQL Server (Entity Framework Core)
* **Authentification :** Keycloak (OpenID Connect)

---

## ✨ Fonctionnalités

### 🔓 Publiques

* **Accueil Hextech :** Page d'accueil immersive.
* **Wiki Champions :** Liste complète des champions avec moteur de recherche.
* **Détails Champion :** Lore, image et astuces communautaires.

### 🔐 Utilisateurs Connectés

* **Dashboard Personnel :** Liaison du compte Riot (simulation API Riot), affichage du niveau et de l’icône.
* **Partage de Tips :** Ajout d’astuces pour chaque champion.

### 🛡️ Administrateurs (Rôle : *Gestionnaire*)

* **Panel Admin :**

  * Synchronisation des données via l’API Riot
  * Réinitialisation de la base de données

---

## 🛠️ Prérequis

* Docker Desktop (lancé)
* .NET 9 SDK
* Git

---

## 🔧 Installation & Lancement

### 1. Cloner le dépôt

```bash
git clone https://github.com/melenedufrenois/MicrosoftFinal.git
cd MicrosoftFinal
```

### 2. Lancer l'application (Aspire)

Placez-vous dans le dossier racine et exécutez :

```bash
dotnet run --project LoLProject.AppHost/LoLProject.AppHost.csproj
```

### 3. Accéder au Dashboard

Une fois lancé, ouvrez le lien **localhost** affiché dans la console pour accéder au Dashboard Aspire.

Vous pourrez y retrouver :

* **Frontend (WebApp)**
* **API (Swagger)**
* **Keycloak (Administration)**

---

## 🔑 Comptes de Test

Si vous avez recréé la base ou utilisez l'import Keycloak fourni :

* **Utilisateur :** `mehdi / mehdi` *(Rôle : admin)* (non fonctionnel pour le role)
* **Admin Keycloak :** `admin / admin`

---

## 🏗️ Architecture

Le projet suit une architecture claire et modulaire :

* **LoLProject.AppHost :** Orchestration Aspire
* **LoLProject.ApiService :** Logique métier + accès BDD (DTOs anti-références)
* **LoLProject.WebApp :** Interface Blazor
* **LoLProject.Persistence :** Modèle de données partagé (EF Core)

---

## 👥 Équipe

Projet réalisé par **Mehdi TRARI & Mélène DUFRENOIS**
