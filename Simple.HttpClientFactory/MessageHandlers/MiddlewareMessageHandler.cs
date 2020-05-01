using System.Net.Http;

namespace Simple.HttpClientFactory.MessageHandlers
{
    internal class MiddlewareMessageHandler : DelegatingHandler
    {
        public MiddlewareMessageHandler(HttpMessageHandler innerHandler) : base(innerHandler){ }
    }
}
