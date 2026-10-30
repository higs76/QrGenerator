using System.Drawing;
using System.Text;

namespace QrGenerator
{
    /// <summary>Format de l'image produite.</summary>
    public enum QrFormat { Png, Gif, Jpeg, Svg }

    /// <summary>Niveau de correction d'erreur : L (7 %), M (15 %), Q (25 %), H (30 %).</summary>
    public enum QrCorrection { L, M, Q, H }

    /// <summary>Options de rendu. Les valeurs par défaut sont celles de qrserver.</summary>
    public sealed class QrOptions
    {
        /// <summary>Côté de l'image, en pixels (l'image est toujours carrée).</summary>
        public int Taille { get; set; } = 250;

        public QrFormat Format { get; set; } = QrFormat.Png;

        public QrCorrection Correction { get; set; } = QrCorrection.L;

        /// <summary>Marge autour du code, en pixels.</summary>
        public int Marge { get; set; } = 1;

        /// <summary>Zone de silence autour du code, en modules.</summary>
        public int ZoneSilence { get; set; }

        public Color Couleur { get; set; } = Color.Black;

        public Color CouleurFond { get; set; } = Color.White;

        /// <summary>Encodage des caractères dans le QR code (UTF-8 par défaut).</summary>
        public Encoding Encodage { get; set; } = new UTF8Encoding(false);

        /// <summary>Type MIME correspondant à <see cref="Format"/>.</summary>
        public string ContentType
        {
            get
            {
                switch (Format)
                {
                    case QrFormat.Gif: return "image/gif";
                    case QrFormat.Jpeg: return "image/jpeg";
                    case QrFormat.Svg: return "image/svg+xml";
                    default: return "image/png";
                }
            }
        }
    }
}
