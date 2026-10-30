using System;
using System.Drawing;
using System.IO;
using System.Text;
using Xunit;
using ZXing;
using ZXing.Common;

namespace QrGenerator.Tests
{
    public class QrCodeGeneratorTests
    {
        [Theory]
        [InlineData("Example")]
        [InlineData("1234567890")]
        [InlineData("https://intranet/doc?id=42&type=facture")]
        [InlineData("Équipe à Noël : 25 € — ça marche ? ÀÉÈÙÇ œ")]
        [InlineData("日本語 😀")]
        public void Le_texte_relu_est_identique(string texte)
        {
            var png = QrCodeGenerator.Generer(texte, new QrOptions { ZoneSilence = 4 });
            Assert.Equal(texte, Lire(png));
        }

        [Fact]
        public void Un_texte_long_est_relu()
        {
            var texte = string.Join(" ", new string('x', 50), "Facture n° 2026-00042", new string('é', 300));
            var png = QrCodeGenerator.Generer(texte, new QrOptions { Taille = 600, ZoneSilence = 4, Correction = QrCorrection.M });
            Assert.Equal(texte, Lire(png));
        }

        [Fact]
        public void Un_texte_trop_long_leve_ArgumentException() =>
            Assert.Throws<ArgumentException>(() => QrCodeGenerator.Generer(new string('a', 5000)));

        [Fact]
        public void Un_texte_vide_leve_ArgumentException() =>
            Assert.Throws<ArgumentException>(() => QrCodeGenerator.Generer(""));

        [Theory]
        [InlineData(QrFormat.Png, 150)]
        [InlineData(QrFormat.Gif, 150)]
        [InlineData(QrFormat.Jpeg, 150)]
        [InlineData(QrFormat.Png, 1000)]
        [InlineData(QrFormat.Png, 37)]
        public void L_image_a_la_taille_exacte_demandee(QrFormat format, int taille)
        {
            var image = QrCodeGenerator.Generer("Example", new QrOptions { Format = format, Taille = taille });
            using (var bmp = Ouvrir(image))
                Assert.Equal(new Size(taille, taille), bmp.Size);
        }

        [Theory]
        [InlineData(QrFormat.Png)]
        [InlineData(QrFormat.Gif)]
        [InlineData(QrFormat.Jpeg)]
        public void Chaque_format_bitmap_est_relu(QrFormat format)
        {
            var image = QrCodeGenerator.Generer("Facture 42", new QrOptions { Format = format, ZoneSilence = 4 });
            Assert.Equal("Facture 42", Lire(image));
        }

        [Fact]
        public void Les_couleurs_sont_respectees_sans_tramage()
        {
            var rouge = Color.FromArgb(255, 0, 0);
            var bleu = Color.FromArgb(0, 0, 255);
            var gif = QrCodeGenerator.Generer("Example", new QrOptions { Format = QrFormat.Gif, Couleur = rouge, CouleurFond = bleu, Marge = 10 });
            using (var bmp = Ouvrir(gif))
            {
                Assert.Equal(bleu.ToArgb(), bmp.GetPixel(0, 0).ToArgb());       // marge
                Assert.Equal(rouge.ToArgb(), bmp.GetPixel(10, 10).ToArgb());    // coin du motif de repérage
            }
        }

        [Fact]
        public void La_marge_est_en_pixels_comme_qrserver()
        {
            var png = QrCodeGenerator.Generer("Example", new QrOptions { Taille = 150, Marge = 20 });
            using (var bmp = Ouvrir(png))
            {
                Assert.Equal(Color.White.ToArgb(), bmp.GetPixel(19, 19).ToArgb());
                Assert.Equal(Color.Black.ToArgb(), bmp.GetPixel(20, 20).ToArgb());
                Assert.Equal(Color.Black.ToArgb(), bmp.GetPixel(129, 20).ToArgb());
                Assert.Equal(Color.White.ToArgb(), bmp.GetPixel(130, 20).ToArgb());
            }
        }

        [Fact]
        public void Le_svg_a_la_taille_demandee_et_des_nombres_invariants()
        {
            var svg = Encoding.UTF8.GetString(QrCodeGenerator.Generer("Example", new QrOptions { Format = QrFormat.Svg, Taille = 150, Marge = 10 }));
            Assert.StartsWith("<svg ", svg);
            Assert.Contains("width=\"150\" height=\"150\"", svg);
            Assert.Contains("scale(6.190476)", svg);   // 130 px / 21 modules, avec un point décimal même en culture française
        }

        [Fact]
        public void Generer_ecrit_dans_un_flux()
        {
            using (var ms = new MemoryStream())
            {
                QrCodeGenerator.Generer("Example", new QrOptions(), ms);
                Assert.Equal(QrCodeGenerator.Generer("Example"), ms.ToArray());
            }
        }

        internal static Bitmap Ouvrir(byte[] image) => new Bitmap(new MemoryStream(image));

        internal static string Lire(byte[] image)
        {
            var lecteur = new BarcodeReader
            {
                AutoRotate = false,
                Options = new DecodingOptions { TryHarder = true, PossibleFormats = new[] { BarcodeFormat.QR_CODE } }
            };
            using (var bmp = Ouvrir(image))
                return lecteur.Decode(bmp)?.Text;
        }
    }
}
