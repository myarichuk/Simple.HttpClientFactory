using System;
using System.Net.Http;

namespace Simple.HttpClientFactory
{
    //credit: some code is adapted from https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/RestClient.cs
    /// <summary>
    /// Provides static methods for creating a factory that produces pre-configured <see cref="HttpClient"/> instances.
    /// </summary>
    public static class HttpClientFactory
    {
        /// <summary>
        /// Instantiates a new HTTP client builder.
        /// </summary>
        public static IHttpClientBuilder Create() => new HttpClientBuilder();

        /// <summary>
        /// Instantiates a new HTTP client builder with the specified additional message handlers added to its processing pipeline.
        /// </summary>
        public static IHttpClientBuilder Create(params DelegatingHandler[] handlers) => new HttpClientBuilder().WithMessageHandlers(handlers);

        /// <summary>
        /// Instantiates a new HTTP client builder with the specified base URL.
        /// </summary>
        public static IHttpClientBuilder Create(string baseUrl) => new HttpClientBuilder().WithBaseUrl(baseUrl);

        /// <summary>
        /// Instantiates a new HTTP client builder with the specified base URL and additional message handlers added to its processing pipeline.
        /// </summary>
        public static IHttpClientBuilder Create(Uri baseUrl) => new HttpClientBuilder().WithBaseUrl(baseUrl);

        /// <summary>
        /// Instantiates a new HTTP client builder with the specified base URL.
        /// </summary>
        public static IHttpClientBuilder Create(Uri baseUrl, params DelegatingHandler[] handlers) => new HttpClientBuilder().WithBaseUrl(baseUrl).WithMessageHandlers(handlers);

        /// <summary>
        /// Instantiates a new HTTP client builder with the specified base URL and additional message handlers added to its processing pipeline.
        /// </summary>
        public static IHttpClientBuilder Create(string baseUrl, params DelegatingHandler[] handlers) => new HttpClientBuilder().WithBaseUrl(baseUrl).WithMessageHandlers(handlers);
    }
}
