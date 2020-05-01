using System;

namespace Simple.HttpClientFactory
{
    internal static class Constants
    {
        public static readonly TimeSpan ConnectionLifeTime = TimeSpan.FromMinutes(1);
        public const int MaxConnectionsPerServer = 20;
    }
}
