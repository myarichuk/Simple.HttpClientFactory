using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Simple.HttpClientFactory
{
    /// <summary>
    /// Defines a factory for producing pre-configured <see cref="HttpClient"/> instances.
    /// </summary>
    public interface IHttpClientBuilder
    {
        /// <summary>
        /// Sets the base URL to use with the client.
        /// </summary>
        IHttpClientBuilder WithBaseUrl(string baseUrl);

        /// <summary>
        /// Sets the base URL to use with the client.
        /// </summary>
        IHttpClientBuilder WithBaseUrl(Uri baseUrl);

        /// <summary>
        /// Adds a default header to be sent with each request.
        /// </summary>
        IHttpClientBuilder WithDefaultHeader(string name, string value);

        /// <summary>
        /// Adds a collection of default headers to be sent with each request.
        /// </summary>
        IHttpClientBuilder WithDefaultHeaders(IDictionary<string, string> headers);

        /// <summary>
        /// Configures one or more SSL certificates to use.
        /// </summary>
        IHttpClientBuilder WithCertificate(params X509Certificate2[] certificate);

        /// <summary>
        /// Configure one or more SSL certificates to use.
        /// </summary>
        IHttpClientBuilder WithCertificates(IEnumerable<X509Certificate2> certificates);

        /// <summary>
        /// Adds one or more Polly policies to the error policy processing pipeline.
        /// </summary>
        /// <remarks>Policies will be evaluated in the order of their configuration.</remarks>
        IHttpClientBuilder WithPolicy(params IAsyncPolicy<HttpResponseMessage>[] policy);

        /// <summary>
        /// Adds multiple Polly policies to the error policy processing pipeline.
        /// </summary>
        /// <remarks>Policies will be evaluated in the order of their configuration.</remarks>
        IHttpClientBuilder WithPolicies(IEnumerable<IAsyncPolicy<HttpResponseMessage>> policies);

        /// <summary>
        /// Specify timeout
        /// </summary>
        IHttpClientBuilder WithTimeout(in TimeSpan timeout);

        /// <summary>
        /// Adds one or more additional message handlers to the processing pipeline.
        /// </summary>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
        IHttpClientBuilder WithMessageHandler(params DelegatingHandler[] handler);

        /// <summary>
        /// Adds additional message handlers to the processing pipeline.
        /// </summary>
        /// <exception cref="T:System.ArgumentNullException">One of items in <paramref name="handlers"/> is <see langword="null"/>.</exception>
        IHttpClientBuilder WithMessageHandlers(IEnumerable<DelegatingHandler> handlers);

        /// <summary>
        /// Adds an exception handler to transform any thrown <see cref="HttpRequestException"/>s into more user friendly ones (or to add more details).
        /// </summary>
        /// <param name="exceptionHandlingPredicate">If the provided delegate evaluates to <see langword="false"/> when executed, the <see cref="HttpRequestException"/> is thrown as-is.</param>
        /// <param name="exceptionHandler">A delegate to transform the active exception. If it returns <see langword="null"/>, the exception will not be thrown.</param>
        /// <param name="requestExceptionEventHandler">An event handler that is called when an <see cref="HttpRequestException"/> is thrown.</param>
        /// <param name="transformedRequestExceptionEventHandler">An event handler that is called when an <see cref="HttpRequestException"/> has been successfully transformed into an <see cref="Exception"/>.</param>
        /// <remarks>This adds a call to <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>, thus ensuring that <see cref="HttpRequestException"/> will get thrown on a non-success response.</remarks>
        IHttpClientBuilder WithMessageExceptionHandler(Func<HttpRequestException, bool> exceptionHandlingPredicate, Func<HttpRequestException, Exception> exceptionHandler, EventHandler<HttpRequestException> requestExceptionEventHandler = null, EventHandler<Exception> transformedRequestExceptionEventHandler = null);

#if NETCOREAPP2_1
        /// <summary>
        /// Configures the primary message handler before the client is instantiated.
        /// </summary>
        /// <param name="configurator">A delegate to configure the primary message handler before the client is instantiated.</param>
        IHttpClientBuilder WithPrimaryMessageHandlerConfigurator(Action<SocketsHttpHandler> configurator);

        /// <summary>
        /// Instantiates the HTTP client, optionally providing additional configuration to its primary message handler.
        /// </summary>
        /// <param name="configurator">Configure the primary message handler before the client is instantiated.</param>
        HttpClient Build(Action<SocketsHttpHandler> configurator = null);

        /// <summary>
        /// Instantiates the HTTP client, substituting the default primary message handler with the specified one.
        /// </summary>
        /// <param name="defaultPrimaryMessageHandler">The message handler to substitute the default primary message handler with.</param>
        HttpClient Build(SocketsHttpHandler defaultPrimaryMessageHandler);
#else
        /// <summary>
        /// Configures the primary message handler before the client is instantiated.
        /// </summary>
        /// <param name="configurator">A delegate to configure the primary message handler before the client is instantiated.</param>
        IHttpClientBuilder WithPrimaryMessageHandlerConfigurator(Action<HttpClientHandler> configurator);

        /// <summary>
        /// Instantiates the HTTP client, optionally providing additional configuration to its primary message handler.
        /// </summary>
        /// <param name="configurator">Configure the primary message handler before the client is instantiated.</param>
        HttpClient Build(Action<HttpClientHandler> configurator = null);

        /// <summary>
        /// Instantiates the HTTP client, substituting the default primary message handler with the specified one.
        /// </summary>
        /// <param name="defaultPrimaryMessageHandler">The message handler to substitute the default primary message handler with.</param>
        HttpClient Build(HttpClientHandler defaultPrimaryMessageHandler);
#endif
    }
}
