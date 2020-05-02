using Polly;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Simple.HttpClientFactory
{
    public interface IHttpClientBuilder
    {
        /// <summary>
        /// Configure one or more SSL certificates to use
        /// </summary>
        IHttpClientBuilder WithCertificate(params X509Certificate2[] certificates);

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
        IHttpClientBuilder WithMessageHandler(DelegatingHandler handler);

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

        #else

        /// <summary>
        /// Actually create the client with the actual message handler to be used
        /// </summary>
        /// <param name="clientHandlerConfigurator">Configure the default message handler before the client is actually created</param>
        HttpClient Build(Action<HttpClientHandler> clientHandlerConfigurator = null);

        #endif
    }
}
