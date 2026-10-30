using System.IO;
using Xunit;

namespace QrGenerator.Tests
{
    /// <summary>Écrit des images dans bin\...\echantillons pour un contrôle visuel et au téléphone.</summary>
    public class Echantillons
    {
        [Fact]
        public void Ecrire_les_echantillons()
        {
            var dossier = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "echantillons");
            Directory.CreateDirectory(dossier);

            void Ecrire(string nom, string texte, QrOptions o) =>
                File.WriteAllBytes(Path.Combine(dossier, nom + "." + (o.Format == QrFormat.Jpeg ? "jpg" : o.Format.ToString().ToLowerInvariant())),
                                   QrCodeGenerator.Generer(texte, o));

            Ecrire("01-example-150", "Example", new QrOptions { Taille = 150 });
            Ecrire("02-accents", "Équipe à Noël : 25 € — ça marche ? ÀÉÈÙÇ œ", new QrOptions { Taille = 300, ZoneSilence = 4 });
            Ecrire("03-url", "https://www.example.com/document?id=2026-00042&type=facture", new QrOptions { Taille = 300, ZoneSilence = 4 });
            Ecrire("04-svg", "Example SVG — été", new QrOptions { Format = QrFormat.Svg, Taille = 300, ZoneSilence = 4 });
            Ecrire("05-couleurs", "Couleurs", new QrOptions { Format = QrFormat.Gif, Taille = 300, Marge = 20, Couleur = System.Drawing.Color.DarkBlue, CouleurFond = System.Drawing.Color.LightYellow });
            Ecrire("06-impression-600", "Haute résolution pour Aspose", new QrOptions { Taille = 600, ZoneSilence = 4, Correction = QrCorrection.M });
            Ecrire("07-jpeg", "JPEG", new QrOptions { Format = QrFormat.Jpeg, Taille = 300, ZoneSilence = 4 });
        }
    }
}
