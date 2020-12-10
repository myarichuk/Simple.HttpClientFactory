using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Simple.HttpClientFactory.Tests
{
    //the code is taken with slight changes from https://github.com/dotnet/aspnetcore/blob/master/src/Shared/CertificateGeneration/CertificateManager.cs#L70
    //note: ASP.Net Core is licensed under Apache License 2.0 (https://github.com/dotnet/aspnetcore/blob/master/LICENSE.txt)
    public static class DefaultDevCert
    {
        private const string AspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";
        private const string AspNetHttpsOidFriendlyName = "ASP.NET Core HTTPS development certificate";


        /// <summary>
        /// Get the default ASP.Net Core developer certificate. 
        /// </summary>
        /// <returns>Default development certificate or null if not found</returns>
        /// <remarks>make sure that the certificate is 'trusted' by running in .Net CLI 'dotnet dev-certs https --trust'</remarks>
        public static X509Certificate2 Get()
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                var certificates = new List<X509Certificate2>();
                certificates.AddRange(store.Certificates.OfType<X509Certificate2>());
                IEnumerable<X509Certificate2> matchingCertificates = certificates;
                matchingCertificates = matchingCertificates.Where(c => HasOid(c, AspNetHttpsOid));
                
                var now = DateTimeOffset.Now;
                var validCertificates = matchingCertificates
                        .Where(c => c.NotBefore <= now &&
                            now <= c.NotAfter &&
                            IsExportable(c)
                            && MatchesVersion(c))
                        .ToArray();

                return validCertificates.FirstOrDefault();
            }
            finally
            {
                store.Close();
            }
        }

        private static bool MatchesVersion(X509Certificate2 c)
        {
            var byteArray = c.Extensions.OfType<X509Extension>()
                .Where(e => string.Equals(AspNetHttpsOid, e.Oid.Value, StringComparison.Ordinal))
                .Single()
                .RawData;

            //assuming AspNetHttpsCertificateVersion == 0 since it is the default at the moment
            //for more details, take a look here: https://github.com/dotnet/aspnetcore/blob/master/src/Shared/CertificateGeneration/CertificateManager.cs#L37
            if ((byteArray.Length == AspNetHttpsOidFriendlyName.Length && byteArray[0] == (byte)'A') || byteArray.Length == 0)
            {
                // No Version set, default to 0
                return true;
            }
            else
            {
                // Version is in the only byte of the byte array.
                return byteArray[0] >= 0;
            }
        }

        private static bool IsExportable(X509Certificate2 c)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return true;
#if XPLAT
                // For the first run experience we don't need to know if the certificate can be exported.
                return true;
#else
                return    c.GetRSAPrivateKey() is RSACryptoServiceProvider rsaPrivateKey && rsaPrivateKey.CspKeyContainerInfo.Exportable
                       || c.GetRSAPrivateKey() is RSACng cngPrivateKey && cngPrivateKey.Key.ExportPolicy == CngExportPolicies.AllowExport;
#endif
        }


        private static bool HasOid(X509Certificate2 certificate, string oid) => certificate.Extensions.OfType<X509Extension>().Any(e => string.Equals(oid, e.Oid.Value, StringComparison.Ordinal));

    }
}
