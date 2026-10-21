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

### Démarche

Le travail est organisé en trois étapes : analyse fonctionnelle, analyse technique, puis réalisation. Cette section présente le cadrage fonctionnel préalable à l’implémentation ; elle ne constitue pas un bilan des fonctionnalités déjà réalisées.

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

Les décisions ci-dessous complètent les exigences explicites du sujet. L’unicité d’une note par utilisateur et par mois, l’absence de modification des coordonnées et du quota, ainsi que le gel de l’historique des utilisateurs supprimés sont des choix de cadrage. L’authentification des administrateurs est un ajout au périmètre initial.

| Sujet | Décision retenue |
|---|---|
| Utilisateurs | Nom, prénom, adresse et quota renseignés à la création, non modifiables. Rue, code postal et ville obligatoires pour l’adresse. Activation/désactivation et suppression logique disponibles. |
| Configuration du quota | Quota individuel entier supérieur ou égal à 1, renseigné à la création et identique pour toutes les périodes. Zéro est exclu : le quota représente un droit à enregistrer au moins une dépense par mois. |
| Notes de frais | Une seule note par utilisateur, mois et année. Intitulé au format « Utilisateur - Mois Année », figé à la création et non éditable. |
| Dépenses | Description obligatoire, non composée uniquement d’espaces, de 50 caractères maximum. Montant en euros strictement positif, avec deux décimales maximum. Adresse de facturation avec rue, code postal et ville obligatoires ; enseigne facultative. |
| Période | La date complète de la dépense détermine son mois et son année de rattachement et doit correspondre à la période de sa note. Saisies rétroactives et dates futures autorisées, aucune clôture ni restriction temporelle n’étant demandée. |
| Affichage des dates | Présentation en français selon le format demandé, par exemple « mercredi 15 octobre 2025 ». |
| Calcul du quota | Nombre de dépenses non supprimées de l’utilisateur pour le mois et l’année concernés, toutes ses notes confondues. Contrôle à la création et lors d’un transfert entrant ; une correction sans changement de période ne consomme aucune place supplémentaire. |
| Quota atteint | Refus de la création ou du transfert entrant avec un message d’erreur explicite. Les modifications sans augmentation du compteur et les suppressions restent possibles, sous réserve des autres règles. |
| Changement de mois | Transfert vers la note existante du même utilisateur pour la période cible, sous contrôle du quota de destination. Sans note de destination, transfert refusé : l’administrateur doit la créer préalablement si l’utilisateur est actif. Le changement de date et de rattachement réussit entièrement ou ne change rien. |
| Utilisateur inactif | Aucune nouvelle note ne peut lui être affectée. Les opérations sur ses notes existantes restent autorisées sous réserve des autres règles. Un transfert vers une note existante reste possible ; si elle n’existe pas, sa création est interdite. |
| Utilisateur supprimé | Utilisateur, notes et dépenses masqués des listes opérationnelles. Historique conservé en base et figé : toute création, modification ou suppression sur cet historique est interdite. Cette règle doit être appliquée côté serveur. |
| Suppression d’une dépense | Suppression logique ; la dépense ne consomme plus le quota du mois concerné. Aucun parcours de restauration n’est prévu. |
| Suppression d’une note | Suppression physique en cascade de toutes ses dépenses, y compris celles déjà supprimées logiquement. Les dépenses correspondantes ne consomment plus le quota. Opération interdite si l’utilisateur est supprimé. |
| Pagination | Cinq dépenses non supprimées par page, triées par date décroissante puis par identifiant afin de garantir un ordre stable lorsque plusieurs dépenses ont la même date. |
| Accès | Authentification des administrateurs et protection de l’API ajoutées au périmètre. Aucun compte de connexion pour les employés bénéficiaires dans cette version. |

### Exceptions retenues

- Le gel de l’historique d’un utilisateur supprimé limite l’édition des dépenses « en tout temps » et la suppression physique de ses notes.
- La suppression physique d’une note entraîne celle de ses dépenses, malgré leur suppression individuelle normalement logique. Les données ainsi supprimées ne seront donc pas disponibles dans de futures archives.
- La conservation des données d’un utilisateur supprimé préserve son historique, mais ne constitue pas un journal d’audit complet des modifications.

### Hypothèse examinée puis écartée

Une restriction des créations au mois courant a été envisagée pour représenter une clôture comptable. Elle n’a pas été retenue : elle n’est pas demandée dans le cahier des charges et serait contournable par une modification de date vers un mois passé. La gestion d’une véritable clôture est proposée comme amélioration.

### Pistes d’amélioration

- **Clôture comptable mensuelle** : encadrer les créations, modifications et suppressions après clôture, avec une procédure de correction et un calendrier de paiement.
- **Justificatifs** : permettre leur rattachement aux dépenses.
- **Plafonnement monétaire** : compléter le quota en nombre par des plafonds mensuels et/ou par dépense. Par exemple, cinq repas à 25 € représentent 125 €, contre 1 250 € pour cinq repas à 250 € ; les deux situations respectent le même quota de cinq dépenses. Le repas est ici un exemple, aucune catégorie de dépense n’étant prévue dans le périmètre initial.
- **Consultation des archives** : proposer une section dédiée permettant de consulter, en lecture seule, les notes de frais et dépenses conservées des utilisateurs supprimés logiquement.
- **Espace de consultation des employés** : permettre aux bénéficiaires de consulter et suivre leurs propres dépenses en lecture seule.
- **Évolution des données utilisateur** : permettre la modification des coordonnées et définir une politique d’évolution des quotas, notamment leur effet sur les périodes passées.
