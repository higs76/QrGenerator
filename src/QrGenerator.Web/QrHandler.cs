using System;
using System.IO;
using System.Web;

namespace QrGenerator.Web
{
    /// <summary>
    /// Remplaçant de https://api.qrserver.com/v1/create-qr-code/ : même syntaxe d'URL, aucune logique propre.
    /// Mappé sur v1/create-qr-code/ dans web.config.
    /// </summary>
    public sealed class QrHandler : IHttpHandler
    {
        static readonly TimeSpan DureeCache = TimeSpan.FromDays(7);

        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            var requete = context.Request;
            var reponse = context.Response;

            // Chaîne brute (encore encodée) : QrServerRequest gère lui-même charset-source.
            var query = requete.ServerVariables["QUERY_STRING"];
            if (requete.HttpMethod == "POST")
                using (var corps = new StreamReader(requete.InputStream))
                    query += "&" + corps.ReadToEnd();

            QrServerRequest qr;
            byte[] image;
            try
            {
                qr = QrServerRequest.Parse(query);
                image = QrCodeGenerator.Generer(qr.Data, qr.Options);
            }
            catch (ArgumentException ex)
            {
                reponse.StatusCode = 400;
                reponse.ContentType = "text/plain";
                reponse.Write(ex.Message);
                return;
            }

            // Une même URL donne toujours la même image : navigateurs et proxies peuvent la garder.
            reponse.ContentType = qr.Options.ContentType;
            reponse.Cache.SetCacheability(HttpCacheability.Public);
            reponse.Cache.SetMaxAge(DureeCache);
            reponse.Cache.SetExpires(DateTime.UtcNow.Add(DureeCache));
            reponse.BinaryWrite(image);
        }
    }
}
