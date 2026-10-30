# QrGenerator — consignes pour Claude

## Le projet
Générateur de QR code **interne** qui remplace le service externe `api.qrserver.com`.
Raison : une coupure Internet a bloqué la génération des documents pendant 4 jours.
L'étude complète se trouve dans `docs/etude-faisabilite-qrcode.md` : la lire avant toute proposition.

## Contraintes
- Langage : **C#**. Cible : **.NET Framework 4.0 minimum** (net40).
- Bibliothèque de calcul : **QRCoder** (MIT), DLL net40 sans dépendance.
- **Aucune dépendance réseau** à l'exécution.
- Deux usages, une seule source de vérité :
  1. **DLL** appelée directement depuis un projet (insertion dans Aspose.Words avec `InsertImage(byte[])`) ;
  2. **site IIS intranet** avec un handler qui accepte la **même syntaxe d'URL que qrserver**
     (`/v1/create-qr-code/?size=150x150&data=...`), pour migrer les appelants en changeant seulement le domaine.

## Façon de travailler
- Phase actuelle : **mock**.
- Le développeur est expérimenté en VB.NET et ASP.NET WebForms. Pour toute notion récente
  (csproj au format SDK, CLI `dotnet`, packages de référence, etc.), **expliquer brièvement** ce que c'est et pourquoi on l'utilise.
- Préférer des solutions concises et élégantes.
- Proposer et échanger avant les gros changements. Répondre en français.
