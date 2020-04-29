using Easy.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.HttpClientFactory
{
    public class HttpClientHandlerEx : HttpClientHandler
    {
        private static readonly TimeSpan ConnectionLifeTime = TimeSpan.FromMinutes(1);

        private readonly HashSet<EndpointCacheKey> _endpoints = new HashSet<EndpointCacheKey>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try{
            return base.SendAsync(request, cancellationToken);
            }
            finally
            {
                EnsureConnectionLeaseTimeout(request.RequestUri);
            }
        }

        private void EnsureConnectionLeaseTimeout(Uri endpoint)
        {
            if (!endpoint.IsAbsoluteUri) { return; }
            
            var key = new EndpointCacheKey(endpoint);
            lock (_endpoints)
            {
                if (_endpoints.Contains(key)) { return; }

                ServicePointManager.FindServicePoint(endpoint)
                    .ConnectionLeaseTimeout = (int)ConnectionLifeTime.TotalMilliseconds;
                _endpoints.Add(key);
            }
        }

        private struct EndpointCacheKey : IEquatable<EndpointCacheKey>
        {
            private readonly Uri _uri;

            public EndpointCacheKey(Uri uri) => _uri = uri;

            public bool Equals(EndpointCacheKey other) => _uri == other._uri;

            public override bool Equals(object obj) => obj is EndpointCacheKey other && Equals(other);

            public override int GetHashCode() => HashHelper.GetHashCode(_uri.Scheme, _uri.DnsSafeHost, _uri.Port);

            public static bool operator ==(EndpointCacheKey left, EndpointCacheKey right) => left.Equals(right);

            public static bool operator !=(EndpointCacheKey left, EndpointCacheKey right) => !left.Equals(right);
        }
    }
}
