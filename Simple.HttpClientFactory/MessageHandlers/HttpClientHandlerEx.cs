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
    /// <summary>
    /// A message handler that caches previously visited URLs.
    /// </summary>
    public class HttpClientHandlerEx : HttpClientHandler
    {
        private readonly ConcurrentDictionary<UriCacheKey, UriCacheKey> _alreadySeenAddresses = new ConcurrentDictionary<UriCacheKey, UriCacheKey>();

        /// <summary>
        /// A list of URL addresses previously visited by the message handler.
        /// </summary>
        public IReadOnlyCollection<UriCacheKey> AlreadySeenAddresses => _alreadySeenAddresses.Values.ToList().AsReadOnly();

        /// <summary>
        /// Creates an instance of  <see cref="T:System.Net.Http.HttpResponseMessage" /> based on the information provided in the <see cref="T:System.Net.Http.HttpRequestMessage" /> as an operation that will not block.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// An object used as a key for URL caching together with the <see cref="HttpClientHandlerEx"/> message handler.
        /// </summary>
        public readonly struct UriCacheKey : IEquatable<UriCacheKey>
        {
            private readonly Uri _uri;

            /// <summary>
            /// Instantiates an object used as a key for URL caching together with the <see cref="HttpClientHandlerEx"/> message handler.
            /// </summary>
            /// <param name="uri">A URL to associate with this cache key.</param>
            public UriCacheKey(string uri) => _uri = new Uri(uri);

            /// <summary>
            /// Instantiates an object used as a key for URL caching together with the <see cref="HttpClientHandlerEx"/> message handler.
            /// </summary>
            /// <param name="uri">A URL to associate with this cache key.</param>
            public UriCacheKey(Uri uri) => _uri = uri;

            /// <summary>
            /// Determines whether this instance and another specified <see cref="UriCacheKey"></see> object are considered to be the same.
            /// </summary>
            /// <param name="obj">The <see cref="UriCacheKey"></see> to compare to this instance.</param>
            public bool Equals(UriCacheKey obj) => _uri == obj._uri;

            /// <summary>
            /// Determines whether this instance and another specified <see cref="UriCacheKey"></see> object are considered to be the same.
            /// </summary>
            /// <param name="obj">The <see cref="UriCacheKey"></see> to compare to this instance.</param>
            public override bool Equals(object obj) => obj is UriCacheKey other && Equals(other);

            /// <summary>
            /// Returns the hash code for this <see cref="UriCacheKey"/>.
            /// </summary>
            /// <returns>A 32-bit signed integer hash code.</returns>
            public override int GetHashCode() => HashCode.Combine(_uri.Scheme, _uri.DnsSafeHost, _uri.Port);

            /// <summary>
            /// Determines whether two specified <see cref="UriCacheKey"></see> objects are considered to be the same.
            /// </summary>
            /// <param name="left">The first <see cref="UriCacheKey"></see> object to compare.</param>
            /// <param name="right">The second <see cref="UriCacheKey"></see> object to compare.</param>
            public static bool operator ==(UriCacheKey left, UriCacheKey right) => left.Equals(right);

            /// <summary>
            /// Determines whether two specified <see cref="UriCacheKey"></see> objects are considered to be different.
            /// </summary>
            /// <param name="left">The first <see cref="UriCacheKey"></see> object to compare.</param>
            /// <param name="right">The second <see cref="UriCacheKey"></see> object to compare.</param>
            public static bool operator !=(UriCacheKey left, UriCacheKey right) => !left.Equals(right);

            /// <summary>
            /// Returns the string representation of this <see cref="UriCacheKey"/>.
            /// </summary>
            public override string ToString() => _uri.ToString();
        }
    }
}
#endif
