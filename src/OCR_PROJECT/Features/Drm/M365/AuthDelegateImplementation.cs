using System.Security.Cryptography.X509Certificates;
using Document.Intelligence.Agent.Features.Drm.M365.Models;
using Microsoft.Identity.Client;
using Microsoft.InformationProtection;

namespace Document.Intelligence.Agent.Features.Drm.M365
{
    internal class AuthDelegateImplementation : IAuthDelegate
    {
        private ApplicationInfo _appInfo;
        private readonly DrmConfig _drmConfig;


        public AuthDelegateImplementation(ApplicationInfo appInfo, DrmConfig drmConfig)
        {
            _appInfo = appInfo;
            _drmConfig = drmConfig;
        }

        public string AcquireToken(Identity identity, string authority, string resource, string claims)
        {
            if (authority.ToLower().Contains("common"))
            {
                var authorityUri = new Uri(authority);
                authority = String.Format("https://{0}/{1}", authorityUri.Host, _drmConfig.IDA.TENANT);
            }

            IConfidentialClientApplication app;

            if (_drmConfig.IDA.DO_CERT_AUTH)
            {
                //Console.WriteLine("Performing certificate based auth with {0}", certThumb);

                // Read cert from local machine
                var certificate = ReadCertificateFromStore(_drmConfig.IDA.CERT_THUMB_PRINT);
                // Use cert to build ClientAssertionCertificate
                app = ConfidentialClientApplicationBuilder.Create(_appInfo.ApplicationId)
                .WithCertificate(certificate)
                .WithRedirectUri(_drmConfig.IDA.REDIRECT_URI)
                .Build();
            }
            else
            {
                //Console.WriteLine("Performing client secret based auth.");
                app = ConfidentialClientApplicationBuilder.Create(_appInfo.ApplicationId)
                .WithClientSecret(_drmConfig.IDA.CLIENT_SECRET)
                .WithRedirectUri(_drmConfig.IDA.REDIRECT_URI)
                .Build();
            }

            string[] scopes = new string[] { resource[resource.Length - 1].Equals('/') ? $"{resource}.default" : $"{resource}/.default" };

            AuthenticationResult authResult = app.AcquireTokenForClient(scopes)
                .WithTenantId(_drmConfig.IDA.TENANT)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();
            // Return the token. The token is sent to the resource.
            return authResult.AccessToken;
        }

        private static X509Certificate2 ReadCertificateFromStore(string thumbprint)
        {
            X509Certificate2 cert = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certCollection = store.Certificates;

            // Find unexpired certificates.
            X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

            // From the collection of unexpired certificates, find the ones with the correct name.
            X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindByThumbprint, thumbprint, false);

            // Return the first certificate in the collection, has the right name and is current.
            cert = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();
            store.Close();
            return cert;
        }
    }
}
