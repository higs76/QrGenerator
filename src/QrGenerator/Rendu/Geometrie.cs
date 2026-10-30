namespace QrGenerator.Rendu
{
    /// <summary>
    /// Place le code dans l'image : modules de taille entière, centrés, les pixels restants s'ajoutant à la marge.
    /// Des modules de largeur variable (5 px, 6 px, 5 px…) font perdre le fil aux lecteurs sur les gros codes.
    /// Seule exception : si un module ferait moins d'un pixel, on étire le code sur toute la place disponible.
    /// </summary>
    struct Geometrie
    {
        /// <summary>Position du premier module, en pixels depuis le bord.</summary>
        public readonly int Debut;

        /// <summary>Largeur occupée par les modules, en pixels.</summary>
        public readonly int Largeur;

        public Geometrie(int modules, QrOptions o)
        {
            int utile = o.Taille - 2 * o.Marge;
            int module = utile / modules;
            Largeur = module >= 1 ? module * modules : utile;
            Debut = o.Marge + (utile - Largeur) / 2;
        }
    }
}
