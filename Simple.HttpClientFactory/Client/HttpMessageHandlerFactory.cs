using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Simple.HttpClientFactory.Client
{
    internal static class HttpMessageHandlerFactory
    {
        private const int MAX_CONNECTION_PER_SERVER = 20;
        private static readonly TimeSpan ConnectionLifeTime = TimeSpan.FromMinutes(1);

        public static HttpMessageHandler Create()
        {
#if NETCOREAPP
            return new SocketsHttpHandler
            {
                // https://github.com/dotnet/corefx/issues/26895
                // https://github.com/dotnet/corefx/issues/26331
                // https://github.com/dotnet/corefx/pull/26839
                PooledConnectionLifetime = ConnectionLifeTime,
                PooledConnectionIdleTimeout = ConnectionLifeTime,
                MaxConnectionsPerServer = MAX_CONNECTION_PER_SERVER
            };
#else
            return new HttpClientHandler
            {
                MaxConnectionsPerServer = MAX_CONNECTION_PER_SERVER
            };
#endif
        }
    }
}
