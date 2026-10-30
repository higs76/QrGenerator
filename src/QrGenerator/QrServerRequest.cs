using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace QrGenerator
{
    /// <summary>
    /// Lit une requête au format qrserver (<c>size=150x150&amp;data=...</c>).
    /// Comme qrserver : un paramètre inconnu ou invalide est ignoré, seul <c>data</c> est obligatoire.
    /// </summary>
    public sealed class QrServerRequest
    {
        public const int TailleMax = 1000;
        public const int MargeMax = 50;
        public const int ZoneSilenceMax = 100;

        public string Data { get; private set; }
        public QrOptions Options { get; private set; }

        /// <summary>Analyse une chaîne de requête brute, encore encodée pour l'URL (avec ou sans « ? »).</summary>
        /// <exception cref="ArgumentException"><c>data</c> absent ou vide.</exception>
        public static QrServerRequest Parse(string query)
        {
            // charset-source indique comment décoder les %XX : on le lit d'abord (sa valeur est ASCII).
            var source = LireEncodage(Decoder(query, Encoding.UTF8), "charset-source") ?? Encoding.UTF8;
            var p = Decoder(query, source);

            string data;
            if (!p.TryGetValue("data", out data) || data.Length == 0)
                throw new ArgumentException("Le paramètre « data » est obligatoire.");

            var o = new QrOptions();
            string v;

            if (p.TryGetValue("size", out v))
            {
                var dims = v.ToLowerInvariant().Split('x');
                int l, h;
                if (dims.Length == 2 && Entier(dims[0], out l) && Entier(dims[1], out h) && l == h && l >= 1 && l <= TailleMax)
                    o.Taille = l;
            }

            if (p.TryGetValue("format", out v))
                switch (v.ToLowerInvariant())
                {
                    case "png": o.Format = QrFormat.Png; break;
                    case "gif": o.Format = QrFormat.Gif; break;
                    case "jpg":
                    case "jpeg": o.Format = QrFormat.Jpeg; break;
                    case "svg": o.Format = QrFormat.Svg; break;
                }

            QrCorrection ecc;
            if (p.TryGetValue("ecc", out v) && v.Length == 1 && Enum.TryParse(v, true, out ecc))
                o.Correction = ecc;

            int n;
            if (p.TryGetValue("margin", out v) && Entier(v, out n) && n >= 0 && n <= MargeMax) o.Marge = n;
            if (p.TryGetValue("qzone", out v) && Entier(v, out n) && n >= 0 && n <= ZoneSilenceMax) o.ZoneSilence = n;
            if (2 * o.Marge >= o.Taille) o.Marge = 0;

            Color c;
            if (p.TryGetValue("color", out v) && Couleur(v, out c)) o.Couleur = c;
            if (p.TryGetValue("bgcolor", out v) && Couleur(v, out c)) o.CouleurFond = c;

            o.Encodage = LireEncodage(p, "charset-target") ?? o.Encodage;

            return new QrServerRequest { Data = data, Options = o };
        }

        static bool Entier(string s, out int n) =>
            int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out n);

        static Encoding LireEncodage(Dictionary<string, string> p, string nom)
        {
            string v;
            if (!p.TryGetValue(nom, out v)) return null;
            switch (v.ToUpperInvariant())
            {
                case "UTF-8": return new UTF8Encoding(false);
                case "ISO-8859-1": return Encoding.GetEncoding("ISO-8859-1");
                default: return null;
            }
        }

        /// <summary>Couleur au format « 255-0-0 », « ff0000 » ou « f00 ».</summary>
        static bool Couleur(string s, out Color c)
        {
            c = Color.Empty;
            var rgb = s.Split('-');
            int r, g, b;
            if (rgb.Length == 3)
            {
                if (!Entier(rgb[0], out r) || !Entier(rgb[1], out g) || !Entier(rgb[2], out b) || r > 255 || g > 255 || b > 255)
                    return false;
            }
            else
            {
                if (s.Length == 3) s = new string(new[] { s[0], s[0], s[1], s[1], s[2], s[2] });
                int hex;
                if (s.Length != 6 || !int.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out hex))
                    return false;
                r = hex >> 16; g = (hex >> 8) & 0xFF; b = hex & 0xFF;
            }
            c = Color.FromArgb(r, g, b);
            return true;
        }

        /// <summary>Décode « a=1&amp;b=%C3%A9 » : « + » vaut espace, les %XX sont des octets dans <paramref name="encodage"/>.</summary>
        static Dictionary<string, string> Decoder(string query, Encoding encodage)
        {
            var p = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var paire in (query ?? "").TrimStart('?').Split('&'))
            {
                if (paire.Length == 0) continue;
                int egal = paire.IndexOf('=');
                var nom = DecoderValeur(egal < 0 ? paire : paire.Substring(0, egal), encodage);
                if (!p.ContainsKey(nom))   // première occurrence prioritaire
                    p[nom] = egal < 0 ? "" : DecoderValeur(paire.Substring(egal + 1), encodage);
            }
            return p;
        }

        static string DecoderValeur(string s, Encoding encodage)
        {
            var octets = new List<byte>(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                int hex;
                if (s[i] == '+')
                    octets.Add((byte)' ');
                else if (s[i] == '%' && i + 2 < s.Length &&
                         int.TryParse(s.Substring(i + 1, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out hex))
                {
                    octets.Add((byte)hex);
                    i += 2;
                }
                else
                    octets.AddRange(encodage.GetBytes(s[i].ToString()));
            }
            return encodage.GetString(octets.ToArray());
        }
    }
}
