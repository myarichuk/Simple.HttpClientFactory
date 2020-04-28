using Easy.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Simple.HttpClientFactory
{
    //credit: some code is adapted from https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/RestClient.cs
    public class HttpClientFactory
    {
        private readonly HashSet<EndpointCacheKey> _endpoints = new HashSet<EndpointCacheKey>();

        public IHttpClientBuilder Create() => new HttpClientBuilder();


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
