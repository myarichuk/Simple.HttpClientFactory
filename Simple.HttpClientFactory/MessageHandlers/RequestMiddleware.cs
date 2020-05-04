using System.Net.Http;

namespace Simple.HttpClientFactory.MessageHandlers
{
    internal class RequestMiddleware : DelegatingHandler
    {
        public RequestMiddleware(HttpMessageHandler innerHandler) : base(innerHandler){ }
    }
}
