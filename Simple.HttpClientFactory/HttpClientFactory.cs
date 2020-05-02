using System.Net.Http;

namespace Simple.HttpClientFactory
{
    //credit: some code is adapted from https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/RestClient.cs
    public static class HttpClientFactory
    {
        /// <summary>
        /// Create HttpClient builder
        /// </summary>
        /// <returns></returns>
        public static IHttpClientBuilder Create() => new HttpClientBuilder();

        /// <summary>
        /// Create HttpClient builder with initial message processing pipeline
        /// </summary>
        /// <param name="handlers">Http message handlers to chain into HttpClient's processing pipeline</param>
        /// <returns></returns>
        public static IHttpClientBuilder Create(params DelegatingHandler[] handlers) =>
            new HttpClientBuilder().WithMessageHandlers(handlers);
    }
}
