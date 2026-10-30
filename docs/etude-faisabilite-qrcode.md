# Générateur de QR code interne — Étude de faisabilité

## 1. Contexte et objectif

- Aujourd'hui, les QR codes sont générés par un service externe : `https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=...`
- L'image est insérée dans un document Aspose.Words, puis exportée en PDF.
- Incident : une coupure Internet a rendu le service injoignable, et **aucun document n'a pu être généré pendant 4 jours**.
- **Objectif : supprimer toute dépendance à Internet** pour générer les QR codes.

## 2. Faisabilité : OK

- Générer un QR code est un pur calcul : encodage, correction d'erreurs Reed-Solomon, placement des modules. Aucun appel réseau n'est nécessaire.
- La bibliothèque retenue est **QRCoder** (licence MIT, gratuite, usage commercial autorisé).
  - La version 1.8.1 cible **net40 sans aucune dépendance**. Elle fonctionne aussi sur 4.5 à 4.8, qui réutilisent la DLL net40.
  - Elle se déploie sous la forme d'un seul fichier `QRCoder.dll`.
  - NuGet ne sert qu'une fois, pour récupérer la DLL. Ensuite, la DLL est conservée dans le dépôt ou dans un dossier `lib/`.
- Alternatives écartées :
  - ZXing.Net : plus lourde, son intérêt principal est la lecture de QR codes.
  - QrCode.Net : abandonnée.
  - Développement maison : coûteux pour aucun gain.

## 3. Les deux modes d'utilisation

```
                 ┌──────────────────────────┐
                 │   QrGenerator.dll (C#)   │  ← une seule source de vérité
                 │   + QRCoder.dll          │
                 └──────────┬───────────────┘
            référence directe │            │ référence
                 ┌───────────┘            └────────────┐
   ┌─────────────▼─────────────┐        ┌──────────────▼──────────────┐
   │ Cas 1 : projet .NET       │        │ Cas 2 : site intranet IIS    │
   │ (génération PDF Aspose)   │        │ handler qr.ashx              │
   │ → byte[] en mémoire       │        │ → même syntaxe URL que       │
   │ → InsertImage(byte[])     │        │   qrserver                   │
   └───────────────────────────┘        └──────────────────────────────┘
```

### Cas 1 : la DLL, appelée en direct

- Le projet appelle `QrGenerator.Generer(texte, options)`, qui renvoie un `byte[]`, puis l'insère avec `DocumentBuilder.InsertImage(...)`.
- Il n'y a **aucun appel HTTP**. C'est le mode le plus robuste et le plus rapide, à privilégier dès qu'on maîtrise le code appelant.
- La DLL est appelable depuis VB.NET et depuis C#.

### Cas 2 : le site web intranet, qui remplace qrserver

- C'est un petit site ASP.NET (.NET 4) sur IIS. Il contient un **handler `.ashx`** : un point d'entrée HTTP léger, sans le cycle de vie d'une page WebForms, qui écrit directement l'image dans la réponse.
- Il accepte **la même syntaxe d'URL que qrserver**. La migration se fait par simple remplacement du début de l'URL :
  - avant : `https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=Example`
  - après : `http://srv-intranet/qr/v1/create-qr-code/?size=150x150&data=Example`
- On peut conserver le chemin `/v1/create-qr-code/` grâce à un mapping de handler dans `web.config`, en pipeline intégré. Seul le nom de domaine change.
- Le handler ne contient aucune logique propre : il lit les paramètres, appelle `QrGenerator.dll` et renvoie le résultat.

## 4. Paramètres à supporter (compatibilité qrserver)

Ces paramètres proviennent de la documentation publique goQR. **Il faudra les revérifier** au moment du mock.

| Paramètre | Exemple | Défaut qrserver | Priorité |
|---|---|---|---|
| `data` | texte encodé en URL | — (obligatoire) | **Indispensable** |
| `size` | `150x150` (en px) | 200x200 | **Indispensable** |
| `format` | png, gif, jpeg/jpg, svg, eps | png | png indispensable, svg utile, eps si utilisé |
| `ecc` | L, M, Q, H | L | Utile |
| `margin` | marge en px | 1 | Utile |
| `qzone` | marge en modules | 0 | Utile |
| `color` / `bgcolor` | `000000`, `255-0-0` | noir / blanc | Secondaire |
| `charset-source` / `charset-target` | UTF-8, ISO-8859-1 | UTF-8 | Secondaire |

Principe : un paramètre inconnu est ignoré, comme chez qrserver, pour ne rien casser chez les appelants.

## 5. Choix techniques

- **Rendu de l'image** : QRCoder fournit la matrice des modules (`QRCodeData.ModuleMatrix`), qu'on dessine nous-mêmes. On obtient ainsi une **taille exacte au pixel près** : QRCoder seul ne sait produire que des multiples de la taille d'un module, alors que `150x150` doit sortir en 150×150. On maîtrise aussi les marges et les couleurs.
  - PNG et GIF passent par `System.Drawing`, qui est présent dans .NET 4. Microsoft ne le garantit pas officiellement sous IIS, mais il fonctionne en pratique pour ce volume.
  - SVG : c'est un simple texte XML construit depuis la matrice. Il ne dépend de rien et il est vectoriel, donc idéal pour l'impression.
- **JPEG déconseillé** : sa compression floute les modules. On le garde uniquement pour la compatibilité.
- **Encodage du texte** : UTF-8 par défaut. Les accents sont à tester impérativement.
- **Qualité d'impression (cas 1)** : générer en haute résolution (par exemple 600 px) et laisser Aspose fixer la taille d'affichage en points. La mise en page reste identique et l'impression est nette.

## 6. Points d'attention

- **Longueur d'URL** : par défaut, IIS limite la chaîne de requête à environ 2 Ko (`maxQueryString`). On l'augmente si besoin dans `web.config`, ou on accepte aussi le POST.
- **HTTPS** : si des appelants exigent du HTTPS, il faudra un certificat interne. Sinon, l'URL passe en http.
- **Garde-fous** : taille maximale (par exemple 1000×1000), longueur maximale du texte, code 400 en cas de paramètre invalide.
- **Cache** : une même URL donne toujours la même image. On ajoute des en-têtes `Cache-Control` pour que navigateurs et proxies évitent de redemander l'image.
- **Point de défaillance** : le site intranet devient un service central. Il est interne, donc bien plus sûr qu'Internet, mais les applications qui le peuvent devraient utiliser la DLL directement (cas 1).
- **Bonus confidentialité** : le contenu des QR codes ne part plus chez un tiers.

## 7. Plan pour le mock

1. **Solution Visual Studio** avec trois projets :
   - `QrGenerator` : bibliothèque C# net40 ;
   - `QrGenerator.Web` : site ASP.NET net40 avec `qr.ashx` ;
   - `QrGenerator.Tests` : console ou tests unitaires.
2. **Bibliothèque** :
   - une classe `QrOptions` (taille, format, ecc, marge, couleurs) ;
   - une méthode `Generer(texte, options)` qui renvoie un `byte[]` ;
   - une méthode `Generer(texte, options, Stream)` pour écrire directement dans une sortie.
3. **Handler** : il analyse les paramètres façon qrserver, appelle la bibliothèque, fixe le `Content-Type` et le cache, et gère les erreurs.
4. **Tests** :
   - lecture au téléphone (PNG et SVG) ;
   - accents et caractères spéciaux ;
   - texte long ;
   - comparaison visuelle avec qrserver pour une même URL ;
   - **génération d'un document avec le réseau Internet coupé**.
5. **Intégration** dans un projet Aspose.Words existant (cas 1) en remplacement de l'appel HTTP.
6. **Déploiement** sur IIS intranet (pool d'applications .NET 4, intégré), puis recherche et remplacement de l'URL chez les appelants (cas 2).

## 8. Questions ouvertes

- Le bout de code actuel qui appelle qrserver et insère l'image : téléchargement dans le code, ou URL dans le modèle Word (`INCLUDEPICTURE`, image liée) ?
- La version d'Aspose.Words.
- La liste des applications qui appellent qrserver aujourd'hui, et les paramètres qu'elles utilisent réellement.
- Le nom du serveur et l'URL intranet cible, et la question http ou https.
