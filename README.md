# Technical-Assessment-Developer-Dotnet-Angular


## Général

Ce TP technique vise à tester vos compétences en Angular et ASP.NET Core. Ne perdez pas de temps sur le design, il ne sera pas évalué.

### Prérequis techniques

Le TP utilise .NET Aspire pour configurer une bdd PostgreSQL, une webapi ASP.NET Core, et un front Angular.

Il vous faudra donc :
- Docker Desktop (ou similaire)
- .NET 9 SDK avec ASP.NET
- nodeJS >= 22.12

### Lancement

Il suffit de lancer le projet Aspire disponible dans `backend\src\AppHost`.

Aspire lance la bdd, le backend et le frontend. 

La console dotnet affiche le lien vers le dashboard Aspire pour consulter les services applicatifs.

### Critères d'évaluation

Nous fournissons ci-dessous les besoins fonctionnels à développer, ainsi que leurs critères d'acceptation.

Pour faciliter notre évaluation, merci de :

- faire des commits réguliers ;
- compléter la section "Mes Notes" à la fin de ce readme.md pour lister les tâches réalisées et éventuelles pistes d'amélioration.

### Rendu du travail

Créer un repo privé sur GitHub et inviter l'utilisateur suivant : n2jsoft-hr-cr

Ce repo doit inclure l'ensemble des fichiers (backend et frontend).


## Besoins fonctionnels

On souhaite mettre en place une solution de gestion des dépenses utilisable par un administrateur (typiquement secrétaire ou comptable) pour des utilisateurs de la société.

Les utilisateurs ne saisissent donc pas directement leurs dépenses ; c'est l'administrateur qui s'en charge.

### Gestion des Users

Un utilisateur doit être affectable à une note de frais ; on dit de cet utilisateur qu'il est l'"utilisateur affecté" à la note de frais.

Il ne peut y avoir qu'un seul utilisateur affecté à une note de frais, mais un utilisateur peut etre affecté à plusieurs notes de frais.

Chaque utilisateur a droit à un nombre maximum de dépenses par mois calendaire.

Un utilisateur peut être actif ou inactif. Seuls les utilisateurs actifs peuvent être affectés à une note de frais.

Un utilisateur a un nom, prénom et une adresse postale.

Un utilisateur peut etre supprimé (suppression logique).

### Gestion des Expenses

Une dépense est définie par les attributs suivants :
- une date au format exemple suivant : "mercredi 15 octobre 2025" ;
- une description : maximum 50 caractères ;
- un montant en euros ;
- une adresse de facturation (enseigne / rue + code postal + ville).

Ces attributs peuvent être éditables en tout temps.

Dans le cas où l'utilisateur a atteint son quota de dépenses au mois, un message d'erreur doit l'en informer et il ne peut pas créer sa nouvelle dépense.

Une dépense peut etre supprimée (suppression logique).

### Gestion des Expense Reports

Une note de frais contient les dépenses d'un utilisateur au mois.

Le format de l'intitulé d'une note de frais doit respecter le format "Utilisateur - Mois Année", par ex : "Juste Leblanc - Octobre 2025".

L'intitulé n'est pas éditable.

Une note de frais peut être supprimable (suppression physique).

La page de visualisation des notes de frais permet d'ajouter une note de frais en sélectionnant le mois et l'utilisateur.

La page de visualisation d'une note de frais permet : 
- d'y ajouter des dépenses ;
- d'afficher les dépenses existantes par lot de 5.


## Mes Notes

### État de réalisation et temps passé

L’API et le frontend sont implémentés. Les parcours de gestion des administrés, des notes de frais et des dépenses, ainsi que la création des comptes administrateurs, ont été validés depuis l’application. Les tests et les builds ont été exécutés par incréments pendant la réalisation.

| Activité | Temps consacré |
|---|---|
| Cadrage fonctionnel et technique | 4 h |
| Réalisation de l’API et de ses tests | 30 h |
| Réalisation du frontend et de ses tests | 6 h |
| **Total déclaré** | **40 h** |

Le temps investi privilégie le cadrage, les invariants métier, les transactions et la testabilité. Le frontend reste volontairement sobre, conformément au sujet.

### Lancement pour l’évaluation

Les commandes suivantes sont à exécuter depuis la racine du dépôt, avec Docker Desktop démarré. Le SDK est fixé par `backend/global.json` à **9.0.318**, avec la politique `latestPatch`. Le frontend nécessite Node.js >= 22.12 ; la configuration Volta du frontend fixe sa version de travail.

Installer les dépendances du frontend, puis lancer le socle complet :

```powershell
cd frontend
npm ci
cd ../backend
dotnet restore
dotnet run --project src/AppHost --launch-profile https
```

Visual Studio permet également de lancer directement le projet `AppHost` avec le profil HTTPS. Lors d’un premier lancement en HTTPS, le certificat de développement .NET doit être approuvé sur le poste ; si nécessaire :

```powershell
dotnet dev-certs https --trust
```

En environnement `Development`, l’API applique ses migrations, initialise les bénéficiaires de démonstration, puis provisionne les rôles et le superadministrateur. PostgreSQL et Redis utilisent des volumes Docker persistants : arrêter et relancer AppHost ne réinitialise pas les données.

Le dashboard Aspire donne accès aux ressources, aux journaux et aux adresses exposées. Depuis l’adresse de `webapi`, ouvrir `/scalar/v1` pour consulter OpenAPI et tester l’API. Les ports attribués par Aspire peuvent varier entre deux lancements : utiliser les adresses du dashboard.

Ouvrir la ressource frontend depuis le dashboard pour accéder à l’application. Le navigateur appelle les routes relatives `/api/...` ; le serveur de développement Angular les relaie vers l’API à partir des adresses injectées par Aspire, sans supprimer le préfixe `/api`. Aucun port d’API n’est codé en dur. Dans ce lancement, les appels du navigateur restent de même origine et ne nécessitent pas de configuration CORS supplémentaire.

Les scripts Nx désactivent Nx Cloud : aucun compte ni accès au cache distant du fournisseur du socle n’est nécessaire. Les changements de sources frontend sont pris en compte par le serveur de développement ; une modification de sa configuration de proxy nécessite de redémarrer la ressource frontend.

L’exception `secure: false` du proxy concerne le certificat de développement local de l’API. Elle ne constitue pas une configuration de déploiement en production.

### Compte administrateur de démonstration

- Email du superadministrateur : **`superadmin@n2jsoft.local`**.
- Mot de passe : généré au premier provisionnement du paramètre Aspire et conservé dans les secrets locaux de l’AppHost.
- Les valeurs utilisées sont visibles dans la configuration de la ressource `webapi` : `BootstrapAdmin__Email` et `BootstrapAdmin__Password`.
- L’initialisation est idempotente : elle crée le compte s’il n’existe pas et assure son rôle. Elle ne réinitialise pas le mot de passe d’un compte déjà présent.
- La clé de signature JWT est également générée par Aspire et conservée localement. Aucun mot de passe de démonstration ni aucune clé réelle n’est versionné.
- Les identifiants de la fixture de tests sont indépendants de ceux du lancement Aspire.

Pour tester dans Scalar : appeler `POST /api/auth/login`, récupérer `accessToken`, puis le renseigner dans le mécanisme d’authentification Bearer de Scalar. `POST /api/auth/logout` révoque uniquement le token courant ; sa réutilisation doit ensuite retourner `401`.

### Exécution des tests

Depuis le dossier `backend` :

```powershell
dotnet build --no-restore
dotnet test tests/WebApi.UnitTests/WebApi.UnitTests.csproj
dotnet test tests/WebApi.IntegrationTests/WebApi.IntegrationTests.csproj
```

Les tests d’intégration utilisent leurs propres conteneurs PostgreSQL et Redis via Testcontainers. Docker Desktop doit fonctionner, mais AppHost n’a pas besoin d’être démarré. Arrêter l’application avant un rebuild pour éviter le verrouillage des fichiers sous Windows.

Depuis le dossier `frontend`, après installation des dépendances :

```powershell
npm run test -- --skip-nx-cache
npm run build -- --skip-nx-cache
```

Les tests Angular utilisent Vitest avec l’environnement de test Angular du socle. Ils ne nécessitent pas le démarrage d’AppHost.

Pour les commandes de migration EF, la factory de conception lit `ConnectionStrings__database` dans le terminal courant. Lors de la génération seule d’une migration, une chaîne de conception suffit ; l’application effective d’une migration en ligne de commande exige la connexion réelle et le port courant de PostgreSQL. Le lancement Aspire applique déjà les migrations en développement : aucune mise à jour manuelle de la base n’est nécessaire pour tester l’application.

### Démarche

Le travail est organisé en trois étapes : analyse fonctionnelle, analyse technique, puis réalisation. Les sections de cadrage conservent les questions et décisions prises avant l’implémentation. Le bilan ci-dessous présente les fonctionnalités livrées, les validations effectuées et les limites acceptées.

### Questions initiales

- Plusieurs notes sont-elles autorisées pour un même utilisateur et un même mois ?
- Le quota porte-t-il sur le nombre de dépenses ou leur montant cumulé ?
- Quelle date détermine le mois de consommation du quota ?
- La date d’une dépense doit-elle correspondre à la période de sa note ?
- La saisie rétroactive est-elle autorisée ?
- Comment traiter un changement de date vers un autre mois ?
- Une dépense supprimée logiquement continue-t-elle à consommer le quota ?
- Quels parcours restent possibles pour un utilisateur inactif ou supprimé ?
- Que deviennent les dépenses lors de la suppression physique d’une note ?
- Comment gérer la modification du quota et du nom d’un utilisateur ?
- Quelles validations appliquer aux montants, aux dates et aux adresses ?

### Acteurs et vocabulaire

- Les **administrateurs** (secrétaires, comptables, etc.) saisissent et gèrent les notes de frais et les dépenses pour les employés bénéficiaires.
- Les **administrés** sont les employés bénéficiaires des notes de frais, appelés utilisateurs dans le sujet. Ils ne disposent pas de compte de connexion dans cette version. Aucun rattachement d’un administré à un administrateur particulier n’est prévu. L’interface utilise le terme « administré » pour éviter la confusion avec l’administrateur connecté. Les noms techniques `User` et `/api/users` restent alignés sur le socle.
- Une **note de frais** (`ExpenseReport`) regroupe les dépenses d’un utilisateur pour un mois et une année.
- Une **dépense** (`Expense`) appartient à une note de frais. Le quota porte sur le nombre de dépenses, et non sur le nombre de notes ou le montant cumulé.

### Décisions fonctionnelles retenues

Les décisions ci-dessous complètent les exigences explicites du sujet. L’autorisation de plusieurs notes par utilisateur et par mois précise notre lecture du sujet. L’absence de modification des coordonnées et du quota, les restrictions liées à l’inactivité et le gel de l’historique des utilisateurs supprimés sont des choix de cadrage. L’authentification, le rôle de superadministrateur et la création de comptes administrateurs sont des ajouts au périmètre initial.

| Sujet | Décision retenue |
|---|---|
| Administrés | Nom, prénom, adresse et quota renseignés à la création, non modifiables. Rue, code postal et ville obligatoires pour l’adresse. Activation/désactivation et suppression logique disponibles. |
| Configuration du quota | Quota individuel entier supérieur ou égal à 1, renseigné à la création et identique pour toutes les périodes. Zéro est exclu : le quota représente un droit à enregistrer au moins une dépense par mois. |
| Notes de frais | Plusieurs notes sont autorisées pour un même utilisateur, mois et année. Intitulé au format « Prénom Nom - Mois Année », par exemple « Juste Leblanc - Octobre 2026 », figé à la création et non éditable. Les intitulés déjà persistés ne sont pas recalculés. Une référence stable, distincte de l’intitulé, permet de différencier les notes dans les listes et les sélections. Le GUID `Id` reste l’identifiant technique des relations et des routes. Un entier `Number`, généré par PostgreSQL, unique et global à toutes les notes, fournit la référence lisible `Note 1`, `Note 2`, etc. Cette référence est affichée séparément de l’intitulé et reste stable après rechargement ou suppression d’autres notes. Les trous de numérotation sont acceptés ; aucune renumérotation n’est effectuée. |
| Dépenses | Description obligatoire, non composée uniquement d’espaces, de 50 caractères maximum. Montant en euros strictement positif, avec deux décimales maximum. Adresse de facturation avec rue, code postal et ville obligatoires ; enseigne facultative. |
| Période | La date complète de la dépense détermine son mois et son année de rattachement et doit correspondre à la période de sa note. Saisies rétroactives et dates futures autorisées, aucune clôture ni restriction temporelle n’étant demandée. |
| Affichage des dates | Interface en français, noms des mois dans les sélecteurs et montants formatés avec la locale `fr-FR`. La liste des dépenses utilise le format compact `dd/MM/yyyy` : il s’agit d’un écart de présentation par rapport à l’exemple long du sujet. Le calendrier HTML natif suit la langue et les conventions du navigateur ; sa valeur transmise à l’API reste `yyyy-MM-dd`. |
| Calcul du quota | Nombre de dépenses non supprimées de l’utilisateur pour le mois et l’année concernés, toutes ses notes confondues. Contrôle à la création et lors d’un transfert entrant ; une correction sans changement de période ne consomme aucune place supplémentaire. |
| Quota atteint | Refus de la création ou du transfert entrant avec un message d’erreur explicite. Les modifications sans augmentation du compteur et les suppressions restent possibles, sous réserve des autres règles. |
| Changement de mois | Transfert vers une note existante du même utilisateur pour la période cible, choisie explicitement par l’administrateur, sous contrôle du quota partagé de destination. Sans note de destination, transfert refusé : l’administrateur doit la créer préalablement si l’utilisateur est actif. Le changement de date et de rattachement réussit entièrement ou ne change rien. |
| Administré inactif | Création de notes, ajout de dépenses et transferts entre notes interdits. Consultation, correction des dépenses dans leur note et période existantes, suppression de dépenses et suppression de notes restent autorisées sous réserve des autres règles. |
| Administré supprimé | Utilisateur, notes et dépenses masqués des listes opérationnelles. Historique conservé en base et figé : toute création, modification ou suppression sur cet historique est interdite. Cette règle doit être appliquée côté serveur. |
| Suppression d’une dépense | Suppression logique ; la dépense ne consomme plus le quota du mois concerné. Aucun parcours de restauration n’est prévu. |
| Suppression d’une note | Suppression physique en cascade de toutes ses dépenses, y compris celles déjà supprimées logiquement. Les dépenses correspondantes ne consomment plus le quota. Opération interdite si l’utilisateur est supprimé. |
| Pagination | Cinq dépenses non supprimées par page, triées par date décroissante puis par identifiant afin de garantir un ordre stable lorsque plusieurs dépenses ont la même date. |
| Accès | Authentification des administrateurs et protection de l’API ajoutées au périmètre. Aucun compte de connexion pour les employés bénéficiaires dans cette version. |

### Exceptions retenues

- L’inactivité interdit également l’ajout de dépenses et les transferts entre notes, au-delà de la seule interdiction explicite d’affecter une nouvelle note. Les corrections restent possibles dans la note et la période existantes.

- Le gel de l’historique d’un utilisateur supprimé limite l’édition des dépenses « en tout temps » et la suppression physique de ses notes.
- La suppression physique d’une note entraîne celle de ses dépenses, malgré leur suppression individuelle normalement logique. Les données ainsi supprimées ne seront donc pas disponibles dans de futures archives.
- La conservation des données d’un utilisateur supprimé préserve son historique, mais ne constitue pas un journal d’audit complet des modifications.

### Hypothèses examinées puis écartées

L’unicité d’une note par utilisateur et par mois a été envisagée puis abandonnée : plusieurs notes sont autorisées et le quota est partagé entre elles. Un quota de dix dépenses peut ainsi être réparti en quatre, trois et trois dépenses sur trois notes. Les intitulés restent conformes au format imposé ; une référence séparée les distingue à l’affichage.

Une restriction des créations au mois courant a été envisagée pour représenter une clôture comptable. Elle n’a pas été retenue : elle n’est pas demandée dans le cahier des charges et serait contournable par une modification de date vers un mois passé. La gestion d’une véritable clôture est proposée comme amélioration.

### Choix techniques retenus

Les choix suivants décrivent l’architecture réalisée et les compromis retenus pour ce test.

#### Architecture et responsabilités

- **Monolithe modulaire** : conservation du projet WebApi fourni et organisation en dossiers, avec un découpage par fonctionnalité. Aucun framework CQRS ni infrastructure distribuée supplémentaire n’est introduit.
- **Domain** : modèles et comportements métier, indépendants des contrats HTTP et des entités EF.
- **Application** : services par fonctionnalité (`UserService`, `ExpenseReportService`, `ExpenseService`), orchestration des cas d’usage et interfaces des services et repositories nécessaires.
- **Infrastructure** : persistance EF Core, configurations, migrations, mappings et implémentations des repositories, transactions et accès Redis.
- **API** : Minimal APIs conservées, contrats de requête/réponse, authentification, autorisation et traduction des résultats applicatifs en réponses HTTP.
- Les dépendances sont contrôlées par convention et revue : le découpage en dossiers dans un même projet ne garantit pas leur respect à la compilation.

#### Trois modèles distincts

| Modèle | Responsabilité |
|---|---|
| Domaine | Exprimer les comportements et préserver les invariants métier. |
| Persistance | Représenter l’état stocké, les relations et les contraintes de base. Des champs techniques peuvent y être ajoutés sans modifier les contrats API. |
| Contrats API | Définir les données acceptées et retournées pour chaque opération, sans exposer les entités EF ou les objets métier directement. |

Des mappings explicites isolent les évolutions du métier, du stockage et des contrats HTTP. Le coût de mapping et de maintenance est accepté en contrepartie de cette séparation. L’approche métier s’inspire du DDD ; la réalisation a suivi une démarche guidée par les tests (TDD), par incréments et refactorisations.

Les objets `UserDetails` et `ExpenseDetails` regroupent les données nécessaires à la construction ou à la modification du modèle métier. Ils ne sont ni les entités EF ni les réponses HTTP, et leur forme immuable ne suffit pas à en faire des Value Objects au sens DDD. Leurs responsabilités restent distinctes de celles des modèles `User` et `Expense`, qui portent les comportements et invariants.

`Expense` porte sa date, sa description, son montant et son adresse de facturation. Elle protège ses règles propres ; le service applicatif coordonne les vérifications concernant l’utilisateur, la note, la période et le quota. Les relations de persistance ne définissent pas à elles seules les frontières des agrégats.

Les adresses utilisent des champs simples. Le quota utilisateur est nommé `MaxExpensesPerMonth` afin d’expliciter qu’il s’agit d’un nombre de dépenses, non d’un plafond monétaire. Cette version ne comporte pas de dates techniques de création ou de dernière modification. Leur ajout éventuel relèverait de la persistance, sans exposition automatique dans les contrats API.

#### Persistance et accès aux données

- Conservation d’**EF Core avec Npgsql**, déjà présents dans le socle, en **Code First**. Les migrations sont relues et versionnées avec le code ; elles accompagnent l’évolution du schéma.
- Repositories spécialisés, sans repository générique et sans exposition d’`IQueryable` ou des entités EF hors de l’infrastructure.
- Clé étrangère obligatoire de `Expense` vers `ExpenseReport`, et de `ExpenseReport` vers `User`.
- Index non unique sur `(UserId, Year, Month)` pour rechercher les notes et calculer le quota mensuel partagé. Aucune contrainte d’unicité sur ce triplet : plusieurs notes du même utilisateur peuvent coexister pour une période. La référence d’affichage est distincte de l’intitulé et identifie une note sans ambiguïté.
- Numérotation des notes par une colonne PostgreSQL `GENERATED ALWAYS AS IDENTITY` (`Number`, entier), avec index unique. L’attribution se fait en base, sans calcul applicatif `MAX + 1`. Après sauvegarde, le service relit la note dans la même transaction pour retourner immédiatement le numéro généré. Les notes préexistantes sont numérotées par migration, sans promesse d’ordre chronologique rétroactif.
- Suppression physique en cascade des dépenses lorsque leur note est supprimée. La suppression logique d’un utilisateur conserve ses relations.
- Nullabilité, champs obligatoires et longueurs configurés sur les modèles persistés ; ces contraintes ne remplacent pas la validation des entrées et des règles métier.
- Pas de lazy loading. Chargements explicites, eager loading lorsque les objets liés sont nécessaires, projections et agrégations LINQ pour les lectures ciblées.
- Comptage des dépenses et somme des montants effectués en base sans charger toute la collection. Aucun compteur de dépenses ni total monétaire dérivé n’est persisté dans `ExpenseReport`. Le champ `Number` est un numéro de référence, pas un compteur de dépenses.
- Le total monétaire de la note est retourné par l’API et couvre toutes ses dépenses non supprimées, indépendamment de la page affichée.

#### Transactions et concurrence

Le service applicatif délimite l’opération transactionnelle via une abstraction implémentée dans l’infrastructure. Les repositories partagent le même `DbContext` et ne sauvegardent pas indépendamment les étapes d’un cas d’usage.

Un **verrou explicite sur la ligne de l’utilisateur bénéficiaire**, acquis dans une transaction courte avant les contrôles décisionnels et les écritures, coordonne toutes les mutations de ses notes et dépenses. Une première lecture de note peut précéder le verrou pour retrouver le bénéficiaire ; les lectures nécessaires sont ensuite renouvelées sous verrou lorsque la ressource a pu changer pendant l’attente. Le contrôle de son état, le comptage du quota et l’écriture se déroulent dans cette même transaction. Le verrou est conservé jusqu’au commit ou au rollback.

Tous les chemins de mutation doivent respecter ce protocole, y compris l’activation/désactivation et la suppression logique de l’utilisateur. Un changement de date et de note est atomique : aucune modification partielle en cas de refus ou d’échec.

L’isolation utilisée est `ReadCommitted`. Les reprises automatiques EF sont explicitement désactivées dans l’intégration Aspire (`DisableRetry = true`) pour conserver ce protocole transactionnel sans rejeu automatique des opérations. Les erreurs transitoires SQL ne sont donc pas automatiquement retentées.

Ce choix sérialise les écritures concernant un même utilisateur, y compris sur des mois différents ; les utilisateurs distincts restent indépendants. Ce compromis privilégie une garantie explicite et simple dans le périmètre du test. Les erreurs d’attente ou de concurrence doivent être traitées sans exposer de détails techniques au client.

#### Validation et contrats HTTP

- **FluentValidation** pour les entrées : champs obligatoires, longueurs, montant strictement positif avec deux décimales maximum, quota entier supérieur ou égal à 1. Validation asynchrone explicite dans le parcours des Minimal APIs.
- Le domaine et les services applicatifs garantissent les règles métier : état de l’utilisateur, cohérence de période, quota et conditions de transfert. Les contraintes de base et les transactions complètent ces contrôles.
- Génération du document **OpenAPI** par ASP.NET Core et interface de consultation/test **Scalar**. Le choix de Scalar ne suppose pas l’abandon de Swagger ou de Swashbuckle.
- Groupes de routes : `/api/users`, `/api/expense-reports`, `/api/expense-reports/{expenseReportId}/expenses`, `/api/auth` et `/api/admin-accounts`. Les dépenses sont exposées sous leur note source.
- Création par `POST` avec réponse `201 Created` et identifiant dans le corps. Les créations de bénéficiaires et de notes fournissent un header `Location` vers leur consultation par identifiant. Les créations de dépenses et de comptes administrateurs retournent le corps sans `Location`. Modification complète des champs éditables d’une dépense par `PUT` ; changement d’état utilisateur par `PUT /api/users/{id}/status` ; suppression par `DELETE` avec réponse `204 No Content`.
- Refus métier attendus représentés par des résultats explicites ; traitement centralisé des exceptions inattendues.
- Les refus métier et de validation gérés par l’application utilisent `ProblemDetails` ou `ValidationProblemDetails`, avec code applicatif stable et erreurs par champ lorsque pertinent. Les réponses produites directement par le framework, notamment certains refus de binding ou d’authentification/autorisation, peuvent ne pas contenir ce code ou ce corps. Le frontend traite également le statut HTTP et ne déduit pas le type d’erreur à partir du texte du message.

| Situation | Statut HTTP |
|---|---|
| Entrée invalide | `400 Bad Request` |
| Authentification absente, expirée ou révoquée | `401 Unauthorized` |
| Droits insuffisants | `403 Forbidden` |
| Ressource inexistante ou masquée après suppression | `404 Not Found` |
| Quota atteint ou conflit avec l’état courant | `409 Conflict` |
| Vérification de révocation indisponible (Redis) | `503 Service Unavailable` ; accès protégé refusé |
| Erreur inattendue | `500 Internal Server Error`, sans détail technique exposé |

La pagination utilise `PagedResult<T>` avec `Items`, `PageNumber`, `PageSize`, `TotalCount` et `TotalPages`. Les pages commencent à 1 ; la taille est imposée à 5 côté serveur dans cette version, tout en étant retournée dans le contrat pour permettre une évolution ultérieure. Un numéro inférieur à 1 retourne `400` avec une erreur de validation, sans résultat paginé. Une page valide au-delà des résultats retourne `200` avec une liste vide et les métadonnées. Le montant total appartient à la réponse spécialisée `ExpensePageResponse`, qui expose les éléments et les métadonnées à plat avec `TotalAmount`, pas au wrapper générique.

#### Authentification et autorisation

- **ASP.NET Core Identity** pour les comptes, mots de passe et rôles ; comptes de connexion distincts des employés bénéficiaires métier.
- Un **superadministrateur** est provisionné après migration par un traitement idempotent. Ses identifiants initiaux sont fournis par configuration secrète, sans mot de passe versionné.
- Le superadministrateur peut créer des comptes administrateurs et gérer les employés bénéficiaires, notes et dépenses. Les administrateurs gèrent les employés, notes et dépenses, mais ne créent pas de comptes administrateurs. Aucun parcours d’inscription publique n’est prévu.
- Authentification par **JWT Bearer**, avec émission des JWT dans l’application ; pas d’utilisation des jetons propriétaires d’Identity.
- Durée de vie de l’access token : **une heure**. Validation de la signature, de l’émetteur, de l’audience et de l’expiration. Claims limités aux besoins de l’API : identité, rôle, identifiant unique du jeton (`jti`) et métadonnées de validité.
- Clé de signature fournie par configuration secrète. Un JWT signé n’est pas chiffré : aucune donnée sensible inutile n’y est ajoutée.
- JWT conservé dans **`sessionStorage`** côté Angular, puis ajouté au header `Authorization` uniquement pour les appels à notre API.
- Aucun refresh token dans ce périmètre ; reconnexion nécessaire après expiration.
- Déconnexion effective du token courant : inscription de son `jti` dans une liste de révocation **Redis** jusqu’à la fin de sa fenêtre de validité acceptée par l’API. Vérification à chaque requête authentifiée ; token révoqué refusé.

#### Sécurité : portée et limites

La configuration doit rester proportionnée au test, sans présenter les protections des frameworks comme une garantie automatique contre toutes les attaques.

- Autorisations vérifiées côté serveur ; les guards Angular assurent seulement la cohérence de navigation.
- Le lancement de développement utilise un proxy Angular de même origine. Si le frontend et l’API sont déployés sur des origines différentes, une politique CORS limitée aux origines nécessaires devra être définie. CORS ne remplace ni l’authentification ni l’autorisation.
- Utilisation des protections de rendu et de sanitisation Angular ; pas de contournement de sécurité ou d’injection directe de HTML non maîtrisé.
- Le JWT n’étant pas envoyé automatiquement comme un cookie, l’authentification Bearer par header réduit le scénario classique de CSRF lié aux cookies. Tout ajout ultérieur d’authentification par cookie imposera de réexaminer cette protection.
- **Limite acceptée** : `sessionStorage` est accessible au JavaScript de l’origine ; une XSS pourrait voler le token. Redis permet de le révoquer, mais n’empêche ni son vol ni son utilisation avant révocation.
- La conservation des révocations Redis jusqu’à expiration doit être assurée. Si la vérification est indisponible, l’accès protégé doit être refusé ; la déconnexion ne doit pas être annoncée comme réussie si la révocation n’a pas été enregistrée.
- La révocation concerne le token courant, pas toutes les sessions du compte. Le durcissement complémentaire, notamment une politique CSP adaptée et une gestion plus complète des sessions, reste une amélioration.
- Supprimer ou perdre le volume Redis efface les révocations. Dans ce cas, il faut également invalider les JWT encore en circulation, par exemple en changeant la clé de signature.

#### Frontend Angular

- Conservation du socle **Angular 20.3 / Nx**, avec `@ngrx/signals` compatible avec cette version ; aucune migration vers Angular 21.
- Organisation par fonctionnalité, composants standalone et chargement des pages à la demande.
- **Reactive Forms typés** : champs obligatoires, description limitée à 50 caractères, montant strictement positif à deux décimales maximum, quota entier positif et validation ergonomique des mots de passe administrateurs. Les validations côté navigateur complètent celles de l’API.
- **Signal Stores** pour les listes, la pagination, les chargements, les sauvegardes et les erreurs. Le store d’authentification est global ; les stores métier sont fournis localement par leurs pages.
- Services HTTP typés et intercepteur Bearer limité aux appels de notre API. Un `401` sur une requête authentifiée invalide la session locale correspondante ; une erreur d’un ancien token ne supprime pas une session plus récente.
- Connexion, déconnexion avec révocation serveur et guards de navigation. Le rôle du JWT est décodé uniquement pour l’ergonomie : lien et écran de création d’administrateurs réservés à SuperAdmin. L’API vérifie les autorisations indépendamment de ces contrôles.
- Gestion des administrés ; chargement automatique des notes lors d’un changement d’administré ou de période ; références lisibles ; formulaire de dépense partagé entre création et modification ; recherche de notes de destination pour les transferts.
- Après mutation, relecture serveur des dépenses et des totaux. Suppression sur la dernière page : retour à la dernière page disponible. Si l’écriture réussit mais que l’actualisation échoue, l’interface distingue ce résultat d’un refus de l’écriture et propose une nouvelle lecture.
- Validations ergonomiques dans les champs, messages métier et erreurs HTTP affichés dans la page. Le frontend ne reproduit pas le comptage décisionnel du quota.
- Styles globaux limités aux éléments communs ; styles de feature et de page pour les mises en page spécifiques. Tables adaptatives et défilement horizontal conservé sur petits écrans.

#### Stratégie de tests et réalisation

Réalisation en **TDD**, par comportements observables : test, implémentation minimale, puis refactorisation.

| Niveau | Outils et objectifs |
|---|---|
| Domaine et validation | xUnit, objets et validateurs réels : invariants, champs et transitions d’état. |
| Cas d’usage | xUnit et NSubstitute pour les dépendances : quota, inactivité, historique figé et transferts. |
| Persistance et concurrence | xUnit et Testcontainers PostgreSQL : relations, cascade, exclusions logiques, atomicité, verrouillage et quota partagé entre plusieurs notes. |
| API et authentification | Tests HTTP et Redis isolé : contrats, statuts, rôles, pagination et révocation. |
| Angular | Vitest et TestBed : clients HTTP, formulaires, Signal Stores, navigation et présentation des erreurs serveur. |

Les tests d’intégration démarrent leurs propres instances PostgreSQL et Redis via **Testcontainers**, avec Docker Desktop. Ils ne dépendent pas des conteneurs applicatifs lancés par Aspire. Les dépendances de test sont référencées dans les projets concernés.

Scénarios prioritaires :

- Deux ajouts simultanés, y compris dans des notes différentes du même utilisateur et du même mois, alors qu’une seule place reste disponible : un seul ajout réussit.
- Plusieurs notes pour le même utilisateur et mois : création autorisée, références distinctes et quota calculé sur l’ensemble des dépenses non supprimées.
- Transfert vers une période au quota atteint : date et rattachement initiaux conservés.
- Utilisateur inactif : création de notes, ajout de dépenses et transferts interdits ; corrections dans la période existante et suppressions autorisées.
- Utilisateur supprimé : aucune mutation de son historique possible.
- Suppression logique d’une dépense : place libérée ; suppression physique d’une note : cascade complète.
- Réutilisation d’un JWT après déconnexion : accès refusé.
- Pagination : requête invalide, page vide et total monétaire indépendant de la page.

### Bilan de réalisation et validation

#### API implémentée

| Fonctionnalité | Réalisation |
|---|---|
| Comptes et sécurité | Provisionnement idempotent des rôles et du superadministrateur, connexion JWT, politiques Admin/SuperAdmin, création d’administrateurs réservée au superadministrateur. |
| Cycle du token | Validité d’une heure, déconnexion avec révocation Redis, refus d’un token révoqué et indisponibilité explicite si Redis ne permet plus la vérification. |
| Bénéficiaires | Création, liste, consultation par identifiant, activation/désactivation et suppression logique. |
| Notes de frais | Création, consultation par identifiant, liste par bénéficiaire et période, référence numérique lisible et stable et suppression physique avec cascade. |
| Dépenses | Création, modification, transfert vers une note existante du même bénéficiaire et suppression logique. |
| Quota | Comptage mensuel partagé entre les notes, exclusion des dépenses supprimées, contrôle des transferts entrants et libération des places après suppression ou transfert sortant. |
| Consultation | Pagination serveur par cinq, ordre stable, nombre total et somme globale indépendants de la page. |
| Persistance | Migrations versionnées, contraintes, relations, volumes persistants et unité de travail avec verrou utilisateur pour les mutations. |
| Contrats | Validation FluentValidation, erreurs métier structurées, OpenAPI et Scalar. |

#### Principales routes

| Méthode | Route | Usage |
|---|---|---|
| `POST` | `/api/auth/login` | Connexion, accessible sans token. |
| `POST` | `/api/auth/logout` | Révocation du token courant. |
| `POST` | `/api/admin-accounts` | Création d’un administrateur, SuperAdmin uniquement. |
| `GET`, `POST` | `/api/users` | Liste et création des bénéficiaires. |
| `GET`, `DELETE` | `/api/users/{id}` | Consultation et suppression logique d’un bénéficiaire. |
| `PUT` | `/api/users/{id}/status` | Activation ou désactivation. |
| `GET` | `/api/expense-reports?userId={id}&year=2026&month=10` | Liste des notes du bénéficiaire pour la période. |
| `POST` | `/api/expense-reports` | Création d’une note. |
| `GET`, `DELETE` | `/api/expense-reports/{id}` | Consultation et suppression physique d’une note. |
| `GET` | `/api/expense-reports/{expenseReportId}/expenses?page=1` | Consultation des dépenses, cinq par page. |
| `POST` | `/api/expense-reports/{expenseReportId}/expenses` | Création d’une dépense. |
| `PUT` | `/api/expense-reports/{expenseReportId}/expenses/{expenseId}` | Modification et éventuel transfert ; `DestinationExpenseReportId` est explicite dans le corps. |
| `DELETE` | `/api/expense-reports/{expenseReportId}/expenses/{expenseId}` | Suppression logique d’une dépense. |

Toutes les routes métier sont réservées aux rôles Admin ou SuperAdmin, à l’exception de la création d’administrateurs, réservée à SuperAdmin. La déconnexion exige un token authentifié. Aucun bénéficiaire métier ne dispose d’un compte de connexion.

#### Validation automatisée et recette navigateur

Les suites unitaires et d’intégration backend, les tests Angular et les builds ont été exécutés par incréments, avec résultats au vert confirmés pendant la réalisation. Aucun pourcentage de couverture ni nombre global de tests n’est revendiqué ici.

Les tests backend couvrent notamment les invariants, mappings, validations HTTP, cas d’usage, filtres de visibilité, contraintes PostgreSQL, cascades, rôles, numérotation des notes et cycle du token Redis. Les tests de concurrence couvrent la compétition pour la dernière place disponible : un seul appel peut la consommer. Les tests frontend couvrent les clients HTTP, sessions, formulaires, stores, pagination et transitions de l’interface.

La recette manuelle a été effectuée depuis le frontend relié à l’API par Aspire :

| Parcours | Résultat vérifié |
|---|---|
| Authentification | Connexion, déconnexion et accès aux pages protégées. Le refus de réutilisation du token révoqué a également été validé lors de la réalisation du cycle JWT. |
| Comptes administrateurs | Création par SuperAdmin, connexion avec le nouveau compte, accès métier et absence d’accès à la création d’administrateurs pour le rôle Admin. |
| Administrés | Création, activation/désactivation, suppression logique et disparition des sélecteurs. |
| Notes de frais | Plusieurs notes pour le même administré et la même période, références numériques distinctes et stables, consultation d’un administré inactif, suppression. |
| Dépenses | Création, correction, affichage et actualisation des totaux. |
| Quota partagé | Dix dépenses réparties en six et quatre sur deux notes du même mois : dixième autorisée, onzième refusée. Suppression d’une dépense : place libérée et nouvel ajout possible. |
| Transferts | Entre notes du même mois et vers un autre mois avec adaptation de la date. Refus si la date ne correspond pas à la période de destination. |
| Pagination | Six dépenses sur deux pages, total indépendant de la page, retour à la page précédente après suppression de l’unique élément de la dernière page. |
| Inactivité | Consultation maintenue ; création de note, ajout de dépense et transfert refusés ; correction dans la note existante et suppressions autorisées. |
| Suppressions | Annulation sans effet ; suppression de dépense avec mise à jour du total ; suppression physique d’une note et cascade sur ses dépenses. Historique d’un administré supprimé inaccessible depuis l’application. |
| Persistance | Données et références conservées après rechargement ; PostgreSQL et Redis utilisent des volumes persistants lors des relances d’AppHost. |

La recette navigateur complète les tests automatisés ; aucun scénario E2E navigateur automatisé n’est revendiqué.

#### Corrections et enseignements de la réalisation

**Transactions et configuration Aspire.** Le premier parcours de désactivation depuis Angular a révélé une incompatibilité entre la stratégie de reprise automatique Npgsql activée par l’intégration Aspire et les transactions explicites du `UnitOfWork`. Les tests d’intégration étaient verts : leur configuration remplaçait l’enregistrement Aspire par un `UseNpgsql` sans reprise automatique. La correction désactive explicitement les reprises (`DisableRetry = true`), tout en conservant transactions et verrous. Le parcours a ensuite été validé. Ce défaut illustre la nécessité de vérifier aussi la configuration réelle d’exécution, au-delà des tests avec dépendances remplacées.

**Identifiant technique et référence fonctionnelle.** Une première version exposait le GUID comme référence. La recette a confirmé que ce choix était peu lisible. Le GUID reste la clé technique ; une migration ajoute un numéro global unique généré par identité PostgreSQL, exposé sous la forme « Note N ». Le numéro est récupéré après sauvegarde et ne change pas lors d’une suppression ou d’un rechargement. Les trous sont acceptés. L’intitulé historique reste inchangé.

**Ergonomie.** Le rechargement manuel après modification des critères a été remplacé par un chargement automatique. Les anciens résultats sont effacés au changement de sélection. Le vocabulaire visible distingue les administrés des administrateurs. Les mois sont nommés et la locale monétaire est française. Les styles spécifiques ont été déplacés dans leurs features.

#### Limites connues et portée de la livraison

- L’audit initial des dépendances frontend a signalé des vulnérabilités. Leur résolution complète n’a pas été réalisée dans ce périmètre ; aucune correction forcée ni absence de vulnérabilité n’est revendiquée. Un audit actualisé et des mises à jour ciblées restent nécessaires.
- Le comptage et la somme utilisent les mêmes filtres que la liste, mais les agrégats et les éléments paginés sont lus par deux requêtes : ils ne forment pas un instantané strict en cas d’écriture concurrente. Le contrôle du quota en écriture reste protégé par la transaction et le verrou.
- La numérotation des notes est globale, croissante par attribution et non continue. Elle n’est ni une numérotation comptable sans rupture ni une preuve de l’ordre des commits.
- Les reprises automatiques SQL sont désactivées ; une erreur transitoire est remontée plutôt que rejouée automatiquement.
- Le stockage JWT dans `sessionStorage`, l’absence de refresh token et la révocation limitée au token courant sont des choix de périmètre documentés ci-dessus.
- Aucun journal d’audit ni aucune date technique de création/modification n’est implémenté. La conservation après suppression logique ne constitue pas un audit complet.
- La liste présente les dates en `dd/MM/yyyy` plutôt que sous la forme longue de l’exemple du sujet. Le calendrier natif dépend de la locale du navigateur, même lorsque l’application est configurée en français.
- Les migrations et le provisionnement au démarrage concernent l’environnement `Development` utilisé pour l’évaluation. Un déploiement hors de cet environnement nécessiterait une procédure explicite et une configuration de sécurité adaptée.

### Pistes d’amélioration

- **Clôture comptable mensuelle** : encadrer les créations, modifications et suppressions après clôture, avec une procédure de correction et un calendrier de paiement.
- **Justificatifs** : permettre leur rattachement aux dépenses.
- **Plafonnement monétaire** : compléter le quota en nombre par des plafonds mensuels et/ou par dépense. Par exemple, cinq repas à 25 € représentent 125 €, contre 1 250 € pour cinq repas à 250 € ; les deux situations respectent le même quota de cinq dépenses. Le repas est ici un exemple, aucune catégorie de dépense n’étant prévue dans le périmètre initial.
- **Consultation des archives** : proposer une section dédiée permettant de consulter, en lecture seule, les notes de frais et dépenses conservées des utilisateurs supprimés logiquement.
- **Espace de consultation des employés** : permettre aux bénéficiaires de consulter et suivre leurs propres dépenses en lecture seule.
- **Évolution des données utilisateur** : permettre la modification des coordonnées et définir une politique d’évolution des quotas, notamment leur effet sur les périodes passées.
- **Audit des modifications** : définir les besoins de traçabilité avant d’ajouter des dates techniques ou un journal d’événements.
- **Cohérence de lecture** : si un instantané strict est nécessaire, assurer une lecture cohérente des agrégats et des éléments paginés.
- **Durcissement de la sécurité** : politique CSP adaptée, gestion de toutes les sessions et réévaluation du stockage des tokens côté navigateur avant une mise en production.
