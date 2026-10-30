using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace QrGenerator.Rendu
{
    /// <summary>Rendu PNG / GIF / JPEG, à la taille exacte demandée (modules de largeur fractionnaire, comme qrserver).</summary>
    static class BitmapRenderer
    {
        public static byte[] Dessiner(bool[,] modules, QrOptions o)
        {
            int n = modules.GetLength(0);
            int taille = o.Taille;

            // Pour chaque pixel d'une ligne (ou colonne) : index du module couvert, -1 dans la marge.
            var index = new int[taille];
            int utile = taille - 2 * o.Marge;
            for (int p = 0; p < taille; p++)
            {
                int q = p - o.Marge;
                index[p] = q < 0 || q >= utile ? -1 : (int)((long)q * n / utile);
            }

            // Image indexée 1 bit : palette [fond, couleur], donc ni dégradé ni tramage.
            using (var bmp = new Bitmap(taille, taille, PixelFormat.Format1bppIndexed))
            {
                var palette = bmp.Palette;
                palette.Entries[0] = o.CouleurFond;
                palette.Entries[1] = o.Couleur;
                bmp.Palette = palette;

                var zone = bmp.LockBits(new Rectangle(0, 0, taille, taille), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
                var ligne = new byte[zone.Stride];
                for (int y = 0; y < taille; y++)
                {
                    System.Array.Clear(ligne, 0, ligne.Length);
                    int my = index[y];
                    if (my >= 0)
                        for (int x = 0; x < taille; x++)
                        {
                            int mx = index[x];
                            if (mx >= 0 && modules[my, mx])
                                ligne[x >> 3] |= (byte)(0x80 >> (x & 7));
                        }
                    Marshal.Copy(ligne, 0, zone.Scan0 + y * zone.Stride, ligne.Length);
                }
                bmp.UnlockBits(zone);

                // GDI+ exige un flux « seekable » pour le PNG : on passe toujours par un MemoryStream.
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, FormatGdi(o.Format));
                    return ms.ToArray();
                }
            }
        }

        static ImageFormat FormatGdi(QrFormat format)
        {
            switch (format)
            {
                case QrFormat.Gif: return ImageFormat.Gif;
                case QrFormat.Jpeg: return ImageFormat.Jpeg;
                default: return ImageFormat.Png;
            }
        }
    }
}
