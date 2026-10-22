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

### Realisation
- Temps Total : 40h
- Temps de cadrage : 4h
- Temps codage API : 30h
- Temps codage Front : 6h

### TEST Account 
- Email et Password sont généré par aspire au démarrage
- les valeurs sont a récupéré dans les propriétés de webapi : BootstrapAdmin__Email et BootstrapAdmin__Password

### Démarche

Le travail est organisé en trois étapes : analyse fonctionnelle, analyse technique, puis réalisation. Cette section présente les cadrages fonctionnel et technique préalables à l’implémentation ; elle ne constitue pas un bilan des fonctionnalités déjà réalisées.

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
- Les **employés bénéficiaires**, appelés utilisateurs dans le sujet, ne saisissent pas directement leurs dépenses. Aucun rattachement d’un employé à un administrateur particulier n’est prévu.
- Une **note de frais** (`ExpenseReport`) regroupe les dépenses d’un utilisateur pour un mois et une année.
- Une **dépense** (`Expense`) appartient à une note de frais. Le quota porte sur le nombre de dépenses, et non sur le nombre de notes ou le montant cumulé.

### Décisions fonctionnelles retenues

Les décisions ci-dessous complètent les exigences explicites du sujet. L’autorisation de plusieurs notes par utilisateur et par mois précise notre lecture du sujet. L’absence de modification des coordonnées et du quota, les restrictions liées à l’inactivité et le gel de l’historique des utilisateurs supprimés sont des choix de cadrage. L’authentification, le rôle de superadministrateur et la création de comptes administrateurs sont des ajouts au périmètre initial.

| Sujet | Décision retenue |
|---|---|
| Utilisateurs | Nom, prénom, adresse et quota renseignés à la création, non modifiables. Rue, code postal et ville obligatoires pour l’adresse. Activation/désactivation et suppression logique disponibles. |
| Configuration du quota | Quota individuel entier supérieur ou égal à 1, renseigné à la création et identique pour toutes les périodes. Zéro est exclu : le quota représente un droit à enregistrer au moins une dépense par mois. |
| Notes de frais | Plusieurs notes sont autorisées pour un même utilisateur, mois et année. Intitulé au format « Utilisateur - Mois Année », figé à la création et non éditable. Une référence stable et unique, distincte de l’intitulé, est affichée à côté pour différencier les notes dans les listes et les sélections. Le mécanisme d’attribution de cette référence sera précisé avant son implémentation. |
| Dépenses | Description obligatoire, non composée uniquement d’espaces, de 50 caractères maximum. Montant en euros strictement positif, avec deux décimales maximum. Adresse de facturation avec rue, code postal et ville obligatoires ; enseigne facultative. |
| Période | La date complète de la dépense détermine son mois et son année de rattachement et doit correspondre à la période de sa note. Saisies rétroactives et dates futures autorisées, aucune clôture ni restriction temporelle n’étant demandée. |
| Affichage des dates | Présentation en français selon le format demandé, par exemple « mercredi 15 octobre 2025 ». |
| Calcul du quota | Nombre de dépenses non supprimées de l’utilisateur pour le mois et l’année concernés, toutes ses notes confondues. Contrôle à la création et lors d’un transfert entrant ; une correction sans changement de période ne consomme aucune place supplémentaire. |
| Quota atteint | Refus de la création ou du transfert entrant avec un message d’erreur explicite. Les modifications sans augmentation du compteur et les suppressions restent possibles, sous réserve des autres règles. |
| Changement de mois | Transfert vers une note existante du même utilisateur pour la période cible, choisie explicitement par l’administrateur, sous contrôle du quota partagé de destination. Sans note de destination, transfert refusé : l’administrateur doit la créer préalablement si l’utilisateur est actif. Le changement de date et de rattachement réussit entièrement ou ne change rien. |
| Utilisateur inactif | Création de notes, ajout de dépenses et transferts entre notes interdits. Consultation, correction des dépenses dans leur note et période existantes, suppression de dépenses et suppression de notes restent autorisées sous réserve des autres règles. |
| Utilisateur supprimé | Utilisateur, notes et dépenses masqués des listes opérationnelles. Historique conservé en base et figé : toute création, modification ou suppression sur cet historique est interdite. Cette règle doit être appliquée côté serveur. |
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

Les choix suivants constituent le cadre de réalisation. Leur implémentation et leur validation seront renseignées dans le bilan final.

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
| Persistance | Représenter l’état stocké, les relations et les champs techniques d’audit. |
| Contrats API | Définir les données acceptées et retournées pour chaque opération, sans exposer les entités EF ou les objets métier directement. |

Des mappings explicites isolent les évolutions du métier, du stockage et des contrats HTTP. Le coût de mapping et de maintenance est accepté en contrepartie de cette séparation. L’approche métier s’inspire du DDD ; la réalisation sera guidée par les tests (TDD).

`Expense` porte sa date, sa description, son montant et son adresse de facturation. Elle protège ses règles propres ; le service applicatif coordonne les vérifications concernant l’utilisateur, la note, la période et le quota. Les relations de persistance ne définissent pas à elles seules les frontières des agrégats.

Les adresses utilisent des champs simples. Le quota utilisateur est nommé `MaxExpensesPerMonth` afin d’expliciter qu’il s’agit d’un nombre de dépenses, non d’un plafond monétaire. Les dates techniques de création et de dernière modification appartiennent au modèle de persistance et ne sont pas exposées sans nécessité.

#### Persistance et accès aux données

- Conservation d’**EF Core avec Npgsql**, déjà présents dans le socle, en **Code First**. Les migrations sont relues et versionnées avec le code ; elles accompagnent l’évolution du schéma.
- Repositories spécialisés, sans repository générique et sans exposition d’`IQueryable` ou des entités EF hors de l’infrastructure.
- Clé étrangère obligatoire de `Expense` vers `ExpenseReport`, et de `ExpenseReport` vers `User`.
- Index non unique sur `(UserId, Year, Month)` pour rechercher les notes et calculer le quota mensuel partagé. Aucune contrainte d’unicité sur ce triplet : plusieurs notes du même utilisateur peuvent coexister pour une période. La référence d’affichage est distincte de l’intitulé et identifie une note sans ambiguïté.
- Suppression physique en cascade des dépenses lorsque leur note est supprimée. La suppression logique d’un utilisateur conserve ses relations.
- Nullabilité, champs obligatoires et longueurs configurés sur les modèles persistés ; ces contraintes ne remplacent pas la validation des entrées et des règles métier.
- Pas de lazy loading. Chargements explicites, eager loading lorsque les objets liés sont nécessaires, projections et agrégations LINQ pour les lectures ciblées.
- Comptage des dépenses et somme des montants effectués en base sans charger toute la collection. Aucun compteur ni total dérivé n’est persisté dans `ExpenseReport`.
- Le total monétaire de la note est retourné par l’API et couvre toutes ses dépenses non supprimées, indépendamment de la page affichée.

#### Transactions et concurrence

Le service applicatif délimite l’opération transactionnelle via une abstraction implémentée dans l’infrastructure. Les repositories partagent le même `DbContext` et ne sauvegardent pas indépendamment les étapes d’un cas d’usage.

Un **verrou explicite sur la ligne de l’utilisateur bénéficiaire**, acquis dans une transaction courte avant les lectures de validation, coordonne toutes les mutations de ses notes et dépenses. Le contrôle de son état, le comptage du quota et l’écriture se déroulent dans cette même transaction. Le verrou est conservé jusqu’au commit ou au rollback.

Tous les chemins de mutation doivent respecter ce protocole, y compris l’activation/désactivation et la suppression logique de l’utilisateur. Un changement de date et de note est atomique : aucune modification partielle en cas de refus ou d’échec.

Ce choix sérialise les écritures concernant un même utilisateur, y compris sur des mois différents ; les utilisateurs distincts restent indépendants. Ce compromis privilégie une garantie explicite et simple dans le périmètre du test. Les erreurs d’attente ou de concurrence doivent être traitées sans exposer de détails techniques au client.

#### Validation et contrats HTTP

- **FluentValidation** pour les entrées : champs obligatoires, longueurs, montant strictement positif avec deux décimales maximum, quota entier supérieur ou égal à 1. Validation asynchrone explicite dans le parcours des Minimal APIs.
- Le domaine et les services applicatifs garantissent les règles métier : état de l’utilisateur, cohérence de période, quota et conditions de transfert. Les contraintes de base et les transactions complètent ces contrôles.
- Génération du document **OpenAPI** par ASP.NET Core et interface de consultation/test **Scalar**. Le choix de Scalar ne suppose pas l’abandon de Swagger ou de Swashbuckle.
- Groupes de routes par ressource : `/api/users`, `/api/expense-reports`, `/api/expenses`, `/api/auth` et `/api/admin-accounts`.
- Création par `POST` avec réponse `201 Created`, identifiant et adresse de la ressource ; modification complète des champs éditables d’une dépense par `PUT` ; changement d’état utilisateur par `PUT /api/users/{id}/status` ; suppression par `DELETE` avec réponse `204 No Content`.
- Refus métier attendus représentés par des résultats explicites ; traitement centralisé des exceptions inattendues.
- Erreurs au format `ProblemDetails` ou `ValidationProblemDetails`, avec code applicatif stable et erreurs par champ lorsque pertinent. Le frontend ne déduit pas le type d’erreur à partir du texte du message.

| Situation | Statut HTTP |
|---|---|
| Entrée invalide | `400 Bad Request` |
| Authentification absente, expirée ou révoquée | `401 Unauthorized` |
| Droits insuffisants | `403 Forbidden` |
| Ressource inexistante ou masquée après suppression | `404 Not Found` |
| Quota atteint ou conflit avec l’état courant | `409 Conflict` |
| Erreur inattendue | `500 Internal Server Error`, sans détail technique exposé |

La pagination utilise `PagedResult<T>` avec `Items`, `PageNumber`, `PageSize` et `TotalCount`. Les pages commencent à 1 ; la taille est imposée à 5 côté serveur dans cette version, tout en étant retournée dans le contrat pour permettre une évolution ultérieure. Un numéro inférieur à 1 retourne `400` avec une erreur de validation, sans résultat paginé. Une page valide au-delà des résultats retourne `200` avec une liste vide et les métadonnées. Le montant total appartient à la réponse spécialisée de détail de note, pas au wrapper générique.

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
- CORS limité aux origines nécessaires lorsque le navigateur appelle l’API depuis une autre origine. CORS n’est ni une authentification ni une protection générale contre les appels externes.
- Utilisation des protections de rendu et de sanitisation Angular ; pas de contournement de sécurité ou d’injection directe de HTML non maîtrisé.
- Le JWT n’étant pas envoyé automatiquement comme un cookie, l’authentification Bearer par header réduit le scénario classique de CSRF lié aux cookies. Tout ajout ultérieur d’authentification par cookie imposera de réexaminer cette protection.
- **Limite acceptée** : `sessionStorage` est accessible au JavaScript de l’origine ; une XSS pourrait voler le token. Redis permet de le révoquer, mais n’empêche ni son vol ni son utilisation avant révocation.
- La conservation des révocations Redis jusqu’à expiration doit être assurée. Si la vérification est indisponible, l’accès protégé doit être refusé ; la déconnexion ne doit pas être annoncée comme réussie si la révocation n’a pas été enregistrée.
- La révocation concerne le token courant, pas toutes les sessions du compte. Le durcissement complémentaire, notamment une politique CSP adaptée et une gestion plus complète des sessions, reste une amélioration.

#### Frontend Angular

- Conservation des versions du socle fourni : **Angular 20.3** et Nx ; aucune migration vers Angular 21 n’est prévue pour le test.
- Organisation par fonctionnalité, composants standalone et chargement des pages à la demande.
- **Reactive Forms typés** pour la saisie, validation ergonomique et affichage des erreurs serveur. Le backend reste l’autorité pour les règles métier.
- **`@ngrx/signals`** pour les stores par fonctionnalité : données, pagination, chargement et erreurs. Les formulaires conservent leur état de saisie ; les stores coordonnent les services HTTP sans reproduire les décisions métier.
- Services HTTP typés et intercepteur d’authentification limité à notre API. Traitement commun des `401`, affichage contextualisé des erreurs de champs et des conflits métier.
- Parcours prévus : connexion, gestion des utilisateurs, liste/création des notes, détail d’une note et gestion de ses dépenses, création des comptes administrateurs réservée au superadministrateur.

#### Stratégie de tests et réalisation

Réalisation en **TDD**, par comportements observables : test, implémentation minimale, puis refactorisation.

| Niveau | Outils et objectifs |
|---|---|
| Domaine et validation | xUnit, objets et validateurs réels : invariants, champs et transitions d’état. |
| Cas d’usage | xUnit et NSubstitute pour les dépendances : quota, inactivité, historique figé et transferts. |
| Persistance et concurrence | xUnit et Testcontainers PostgreSQL : relations, cascade, exclusions logiques, atomicité, verrouillage et quota partagé entre plusieurs notes. |
| API et authentification | Tests HTTP et Redis isolé : contrats, statuts, rôles, pagination et révocation. |
| Angular | Outillage de test du socle : formulaires, transitions des Signal Stores et présentation des erreurs serveur. |

Les tests d’intégration démarrent leurs propres instances PostgreSQL et Redis via **Testcontainers**, avec Docker Desktop. Ils ne dépendent pas des conteneurs applicatifs lancés par Aspire. Les dépendances de test seront ajoutées aux projets concernés.

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

À compléter après implémentation : fonctionnalités effectivement réalisées, tests exécutés et résultats, commandes de lancement et de test, configuration nécessaire et éventuels écarts au cadrage. Aucun test ni mécanisme prévu ci-dessus n’est présenté à ce stade comme déjà exécuté ou livré.

### Pistes d’amélioration

- **Clôture comptable mensuelle** : encadrer les créations, modifications et suppressions après clôture, avec une procédure de correction et un calendrier de paiement.
- **Justificatifs** : permettre leur rattachement aux dépenses.
- **Plafonnement monétaire** : compléter le quota en nombre par des plafonds mensuels et/ou par dépense. Par exemple, cinq repas à 25 € représentent 125 €, contre 1 250 € pour cinq repas à 250 € ; les deux situations respectent le même quota de cinq dépenses. Le repas est ici un exemple, aucune catégorie de dépense n’étant prévue dans le périmètre initial.
- **Consultation des archives** : proposer une section dédiée permettant de consulter, en lecture seule, les notes de frais et dépenses conservées des utilisateurs supprimés logiquement.
- **Espace de consultation des employés** : permettre aux bénéficiaires de consulter et suivre leurs propres dépenses en lecture seule.
- **Évolution des données utilisateur** : permettre la modification des coordonnées et définir une politique d’évolution des quotas, notamment leur effet sur les périodes passées.
- **Durcissement de la sécurité** : politique CSP adaptée, gestion de toutes les sessions et réévaluation du stockage des tokens côté navigateur avant une mise en production.
