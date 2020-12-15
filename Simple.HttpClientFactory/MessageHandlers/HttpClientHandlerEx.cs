#if NETSTANDARD2_0
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.HttpClientFactory.MessageHandlers
{
    //credit: the idea for storing visited addresses in hash set is taken from https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/RestClient.cs#L570
    //note: Easy.Common is licensed with MIT License (https://github.com/NimaAra/Easy.Common/blob/master/LICENSE)
    public class HttpClientHandlerEx : HttpClientHandler
    {
        private readonly ConcurrentDictionary<UriCacheKey, UriCacheKey> _alreadySeenAddresses = new ConcurrentDictionary<UriCacheKey, UriCacheKey>();

        public IReadOnlyCollection<UriCacheKey> AlreadySeenAddresses => _alreadySeenAddresses.Values.ToList().AsReadOnly();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return base.SendAsync(request, cancellationToken);
            }
            finally
            {
                EnsureConnectionLeaseTimeout(request.RequestUri);
            }
        }

        private void EnsureConnectionLeaseTimeout(Uri endpoint)
        {
            var key = new UriCacheKey(endpoint);

            _alreadySeenAddresses.TryAdd(key, key);

            ServicePointManager.FindServicePoint(endpoint).ConnectionLeaseTimeout = (int)Constants.ConnectionLifeTime.TotalMilliseconds;
        }

        public readonly struct UriCacheKey : IEquatable<UriCacheKey>
        {
            private readonly Uri _uri;

            public UriCacheKey(string uri) => _uri = new Uri(uri);

            public UriCacheKey(Uri uri) => _uri = uri;

            public bool Equals(UriCacheKey other) => _uri == other._uri;

            public override bool Equals(object obj) => obj is UriCacheKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(_uri.Scheme, _uri.DnsSafeHost, _uri.Port);

            public static bool operator ==(UriCacheKey left, UriCacheKey right) => left.Equals(right);

            public static bool operator !=(UriCacheKey left, UriCacheKey right) => !left.Equals(right);


            public override string ToString() => _uri.ToString();
        }
    }
}
#endif
