using System.Drawing;
using System.Globalization;
using System.Text;

namespace QrGenerator.Rendu
{
    /// <summary>Rendu SVG : vectoriel, à la taille exacte demandée, idéal pour l'impression.</summary>
    static class SvgRenderer
    {
        public static byte[] Dessiner(bool[,] modules, QrOptions o)
        {
            int n = modules.GetLength(0);
            var inv = CultureInfo.InvariantCulture;
            double echelle = (double)(o.Taille - 2 * o.Marge) / n;

            var svg = new StringBuilder();
            svg.AppendFormat(inv,
                "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{0}\" height=\"{0}\" viewBox=\"0 0 {0} {0}\" shape-rendering=\"crispEdges\">",
                o.Taille);
            svg.AppendFormat(inv, "<rect width=\"{0}\" height=\"{0}\" fill=\"{1}\"/>", o.Taille, Hex(o.CouleurFond));
            svg.AppendFormat(inv, "<path transform=\"translate({0} {0}) scale({1:0.######})\" fill=\"{2}\" d=\"", o.Marge, echelle, Hex(o.Couleur));

            // Une ligne de modules foncés consécutifs = un seul rectangle.
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    if (!modules[y, x]) continue;
                    int debut = x;
                    while (x + 1 < n && modules[y, x + 1]) x++;
                    svg.AppendFormat(inv, "M{0} {1}h{2}v1h-{2}z", debut, y, x - debut + 1);
                }

            svg.Append("\"/></svg>");
            return new UTF8Encoding(false).GetBytes(svg.ToString());
        }

        static string Hex(Color c) => "#" + c.R.ToString("x2") + c.G.ToString("x2") + c.B.ToString("x2");
    }
}
