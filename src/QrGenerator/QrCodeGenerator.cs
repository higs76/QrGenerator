using System;
using System.IO;
using System.Linq;
using QRCoder;
using QrGenerator.Rendu;

namespace QrGenerator
{
    /// <summary>Point d'entrée : génère l'image d'un QR code, sans aucun accès réseau.</summary>
    public static class QrCodeGenerator
    {
        /// <summary>Génère le QR code de <paramref name="texte"/> et renvoie l'image.</summary>
        /// <exception cref="ArgumentException">Texte vide ou trop long, options invalides.</exception>
        public static byte[] Generer(string texte, QrOptions options = null)
        {
            options = options ?? new QrOptions();
            Valider(options);
            var modules = CalculerModules(texte, options);

            return options.Format == QrFormat.Svg
                ? SvgRenderer.Dessiner(modules, options)
                : BitmapRenderer.Dessiner(modules, options);
        }

        /// <summary>Génère le QR code de <paramref name="texte"/> et l'écrit dans <paramref name="sortie"/>.</summary>
        public static void Generer(string texte, QrOptions options, Stream sortie)
        {
            if (sortie == null) throw new ArgumentNullException(nameof(sortie));
            var image = Generer(texte, options);
            sortie.Write(image, 0, image.Length);
        }

        static void Valider(QrOptions o)
        {
            if (o.Taille < 1) throw new ArgumentException("La taille doit être positive.");
            if (o.Marge < 0 || 2 * o.Marge >= o.Taille) throw new ArgumentException("La marge est incompatible avec la taille.");
            if (o.ZoneSilence < 0) throw new ArgumentException("La zone de silence ne peut pas être négative.");
            if (o.Encodage == null) throw new ArgumentException("L'encodage est obligatoire.");
        }

        /// <summary>Matrice des modules (true = foncé), zone de silence demandée incluse.</summary>
        static bool[,] CalculerModules(string texte, QrOptions options)
        {
            if (string.IsNullOrEmpty(texte)) throw new ArgumentException("Le texte à encoder est vide.");

            var ecc = (QRCodeGenerator.ECCLevel)Enum.Parse(typeof(QRCodeGenerator.ECCLevel), options.Correction.ToString());
            QRCodeData data;
            try
            {
                // Texte ASCII : QRCoder choisit le mode le plus compact (numérique, alphanumérique ou octets).
                // Sinon : octets bruts dans l'encodage demandé, sans en-tête ECI, comme qrserver.
                data = texte.All(c => c < 128)
                    ? new QRCodeGenerator().CreateQrCode(texte, ecc)
                    : new QRCodeGenerator().CreateQrCode(options.Encodage.GetBytes(texte), ecc);
            }
            catch (QRCoder.Exceptions.DataTooLongException ex)
            {
                throw new ArgumentException("Le texte est trop long pour un QR code.", ex);
            }

            using (data)
            {
                // La matrice de QRCoder contient sa propre zone de silence de 4 modules : on la retire.
                var brute = data.ModuleMatrix;
                int cote = 17 + 4 * data.Version;   // norme QR : 21 modules en version 1, +4 par version
                int retrait = (brute.Count - cote) / 2;
                int zone = options.ZoneSilence;

                var modules = new bool[cote + 2 * zone, cote + 2 * zone];
                for (int y = 0; y < cote; y++)
                    for (int x = 0; x < cote; x++)
                        modules[y + zone, x + zone] = brute[y + retrait][x + retrait];
                return modules;
            }
        }
    }
}
