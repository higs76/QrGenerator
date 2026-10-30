using System;
using System.Drawing;
using Xunit;

namespace QrGenerator.Tests
{
    public class QrServerRequestTests
    {
        [Fact]
        public void Les_valeurs_par_defaut_sont_celles_de_qrserver()
        {
            var r = QrServerRequest.Parse("data=Example");
            Assert.Equal("Example", r.Data);
            Assert.Equal(250, r.Options.Taille);
            Assert.Equal(QrFormat.Png, r.Options.Format);
            Assert.Equal(QrCorrection.L, r.Options.Correction);
            Assert.Equal(1, r.Options.Marge);
            Assert.Equal(0, r.Options.ZoneSilence);
        }

        [Fact]
        public void L_url_complete_de_qrserver_est_comprise()
        {
            var r = QrServerRequest.Parse("?size=150x150&data=Hello+world%21&format=SVG&ecc=h&margin=10&qzone=4&color=255-0-0&bgcolor=00f");
            Assert.Equal("Hello world!", r.Data);
            Assert.Equal(150, r.Options.Taille);
            Assert.Equal(QrFormat.Svg, r.Options.Format);
            Assert.Equal(QrCorrection.H, r.Options.Correction);
            Assert.Equal(10, r.Options.Marge);
            Assert.Equal(4, r.Options.ZoneSilence);
            Assert.Equal(Color.FromArgb(255, 0, 0).ToArgb(), r.Options.Couleur.ToArgb());
            Assert.Equal(Color.FromArgb(0, 0, 255).ToArgb(), r.Options.CouleurFond.ToArgb());
        }

        [Theory]
        [InlineData("size=150x100")]     // non carrée
        [InlineData("size=1001x1001")]   // au-delà du maximum
        [InlineData("size=150")]
        [InlineData("size=abc")]
        [InlineData("format=bmp&ecc=Z&margin=abc&qzone=-1&color=zzz&inconnu=1")]
        public void Un_parametre_invalide_est_ignore(string parametres)
        {
            var r = QrServerRequest.Parse("data=x&" + parametres);
            var defaut = new QrOptions();
            Assert.Equal(defaut.Taille, r.Options.Taille);
            Assert.Equal(defaut.Format, r.Options.Format);
            Assert.Equal(defaut.Correction, r.Options.Correction);
            Assert.Equal(defaut.Marge, r.Options.Marge);
            Assert.Equal(defaut.ZoneSilence, r.Options.ZoneSilence);
            Assert.Equal(defaut.Couleur.ToArgb(), r.Options.Couleur.ToArgb());
        }

        [Theory]
        [InlineData("")]
        [InlineData("size=150x150")]
        [InlineData("size=150x150&data=")]
        public void Data_absent_ou_vide_leve_ArgumentException(string query) =>
            Assert.Throws<ArgumentException>(() => QrServerRequest.Parse(query));

        [Fact]
        public void Les_accents_utf8_sont_decodes() =>
            Assert.Equal("Noël à 25 €", QrServerRequest.Parse("data=No%C3%ABl%20%C3%A0%2025%20%E2%82%AC").Data);

        [Fact]
        public void Charset_source_iso_8859_1_est_respecte() =>
            Assert.Equal("Noël", QrServerRequest.Parse("charset-source=ISO-8859-1&data=No%EBl").Data);

        [Fact]
        public void Charset_target_iso_8859_1_est_respecte() =>
            Assert.Equal("ISO-8859-1", QrServerRequest.Parse("data=x&charset-target=iso-8859-1").Options.Encodage.WebName.ToUpperInvariant());

        [Fact]
        public void Le_rendu_de_bout_en_bout_est_relu()
        {
            var r = QrServerRequest.Parse("size=300x300&qzone=4&data=Facture%20n%C2%B0%2042%20%E2%80%94%20%C3%A9t%C3%A9");
            Assert.Equal("Facture n° 42 — été", QrCodeGeneratorTests.Lire(QrCodeGenerator.Generer(r.Data, r.Options)));
        }
    }
}
