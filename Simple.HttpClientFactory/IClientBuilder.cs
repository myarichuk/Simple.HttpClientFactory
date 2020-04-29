using Polly;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Simple.HttpClientFactory
{
    public interface IHttpClientBuilder
    {
        IHttpClientBuilder WithCertificate(params X509Certificate2[] certificates);

        IHttpClientBuilder WithPolicy(IAsyncPolicy<HttpResponseMessage> policy);

        IHttpClientBuilder WithConnectionTimeout(in TimeSpan connectionTimeout);

        #if NETCOREAPP2_1

        HttpClient Build(Action<SocketsHttpHandler> clientHandlerConfigurator = null);

        #else

        HttpClient Build(Action<HttpClientHandler> clientHandlerConfigurator = null);

        #endif
    }
}
