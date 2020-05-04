using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Simple.HttpClientFactory
{
    public interface IHttpClientBuilder
    {
        /// <summary>
        /// Add default headers to be added to each request
        /// </summary>
        IHttpClientBuilder WithDefaultHeader(string name, string value);

        /// <summary>
        /// Add default headers to be added to each request
        /// </summary>
        IHttpClientBuilder WithDefaultHeaders(IReadOnlyDictionary<string, string> headers);

        /// <summary>
        /// Configure one or more SSL certificates to use
        /// </summary>
        IHttpClientBuilder WithCertificates(IEnumerable<X509Certificate2> certificates);

        /// <summary>
        /// Configure one or more SSL certificates to use
        /// </summary>
        IHttpClientBuilder WithCertificate(params X509Certificate2[] certificates);

        /// <summary>
        /// Chain multiple Polly error policies
        /// </summary>
        /// <remarks>Policies will be evaluated in the order of their configuration</remarks>
        IHttpClientBuilder WithPolicies(IEnumerable<IAsyncPolicy<HttpResponseMessage>> policies);

        /// <summary>
        /// Chain Polly error policy
        /// </summary>
        /// <remarks>Policies will be evaluated in the order of their configuration</remarks>
        IHttpClientBuilder WithPolicy(IAsyncPolicy<HttpResponseMessage> policy);

        /// <summary>
        /// Specify timeout
        /// </summary>
        IHttpClientBuilder WithTimeout(in TimeSpan timeout);

        /// <summary>
        /// Add http message handler to processing pipeline
        /// </summary>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handler"/> is <see langword="null"/></exception>
        IHttpClientBuilder WithMessageHandler(DelegatingHandler handler);

        /// <summary>
        /// Add multiple http message handlers to processing pipeline
        /// </summary>
        /// <exception cref="T:System.ArgumentNullException">One of items in <paramref name="handlers"/> is <see langword="null"/></exception>
        IHttpClientBuilder WithMessageHandlers(IEnumerable<DelegatingHandler> handlers);

        /// <summary>
        /// Add exception handler to possibly transform thrown <see cref="HttpRequestException"/> into more user-friendly ones (or add more details)
        /// </summary>
        /// <param name="exceptionHandlingPredicate">If returns false, then the <see cref="HttpRequestException"/> will get thrown as is</param>
        /// <param name="exceptionHandler">Transform the exception. If this returns null, the exception will not get thrown</param>
        /// <remarks>This adds invocation of <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>, 
        ///  thus ensuring that <see cref="HttpRequestException"/> will get thrown on non-success response</remarks>
        IHttpClientBuilder WithMessageExceptionHandler(
                Func<HttpRequestException, bool> exceptionHandlingPredicate,
                Func<HttpRequestException, Exception> exceptionHandler);

        #if NETCOREAPP2_1

        /// <summary>
        /// Actually create the client with the actual message handler to be used
        /// </summary>
        /// <param name="clientHandlerConfigurator">Configure the default message handler before the client is actually created</param>
        HttpClient Build(Action<SocketsHttpHandler> clientHandlerConfigurator = null);

        /// <summary>
        /// Actually create the client with the actual message handler to be used
        /// </summary>
        /// <param name="clientHandler">Client handler instance to use</param>
        HttpClient Build(SocketsHttpHandler clientHandler);

        #else

        /// <summary>
        /// Actually create the client with the actual message handler to be used
        /// </summary>
        /// <param name="clientHandlerConfigurator">Configure the default message handler before the client is actually created</param>
        HttpClient Build(Action<HttpClientHandler> clientHandlerConfigurator = null);

        /// <summary>
        /// Actually create the client with the actual message handler to be used
        /// </summary>
        /// <param name="clientHandler">Client handler instance to use</param>
        HttpClient Build(HttpClientHandler clientHandler);

        #endif
    }
}
