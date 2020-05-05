using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
#if NETCOREAPP2_1
using System.Net.Security;
#endif
using System.Security.Cryptography.X509Certificates;
using Simple.HttpClientFactory.MessageHandlers;

namespace Simple.HttpClientFactory
{
    internal class HttpClientBuilder : IHttpClientBuilder
    {        
        private TimeSpan? _timeout;
        private Uri _baseUrl;
        private readonly List<X509Certificate2> _certificates = new List<X509Certificate2>();
        private readonly List<IAsyncPolicy<HttpResponseMessage>> _policies = new List<IAsyncPolicy<HttpResponseMessage>>();
        private readonly List<DelegatingHandler> _middlewareHandlers = new List<DelegatingHandler>();
        private readonly Dictionary<string, string> _defaultHeaders = new Dictionary<string, string>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IHttpClientBuilder WithDefaultHeader(string name, string value)
        {
            if(!_defaultHeaders.ContainsKey(name))
                _defaultHeaders.Add(name, value);

            return this;
        }

        public IHttpClientBuilder WithBaseUrl(Uri baserUrl)
        {
            _baseUrl = new Uri(baserUrl.ToString());
            return this;
        }

        public IHttpClientBuilder WithDefaultHeaders(IReadOnlyDictionary<string, string> headers)
        {
            foreach(var kvp in headers)
                WithDefaultHeader(kvp.Key, kvp.Value);

            return this;
        }

        public IHttpClientBuilder WithCertificates(IEnumerable<X509Certificate2> certificates)
        {                        
            _certificates.AddRange(certificates);
            return this;
        }
        
        public IHttpClientBuilder WithCertificate(params X509Certificate2[] certificates)
        {                        
            _certificates.AddRange(certificates);
            return this;
        }

        public IHttpClientBuilder WithPolicies(IEnumerable<IAsyncPolicy<HttpResponseMessage>> policies)
        {
            _policies.AddRange(policies);
            return this;
        }


        public IHttpClientBuilder WithPolicy(IAsyncPolicy<HttpResponseMessage> policy)
        {
            _policies.Add(policy);
            return this;
        }

        public IHttpClientBuilder WithTimeout(in TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        public IHttpClientBuilder WithMessageExceptionHandler(
                Func<HttpRequestException, bool> exceptionHandlingPredicate,
                Func<HttpRequestException, Exception> exceptionHandler) =>
            WithMessageHandler(new ExceptionTranslatorRequestMiddleware(exceptionHandlingPredicate, exceptionHandler));

        /// <exception cref="T:System.ArgumentNullException"><paramref name="handler"/> is <see langword="null"/></exception>
        public IHttpClientBuilder WithMessageHandler(DelegatingHandler handler)
        {
            if(handler == null)
                throw new ArgumentNullException(nameof(handler));
            if(_middlewareHandlers.Count > 0)
                _middlewareHandlers.Last().InnerHandler = handler;
            _middlewareHandlers.Add(handler);
            return this;
        }

        public IHttpClientBuilder WithMessageHandlers(IEnumerable<DelegatingHandler> handlers)
        {
            foreach(var handler in handlers)
                WithMessageHandler(handler);
            return this;
        }

        #if NETCOREAPP2_1

        public HttpClient Build(SocketsHttpHandler clientHandler)
        {
            InitializeClientHandler(clientHandler, out var rootPolicyHandler);
            return ConstructClientWithMiddleware(clientHandler, rootPolicyHandler);
        }

        //ServicePointManager in .Net Core is a no-op so we need to do this
        //see https://github.com/dotnet/extensions/issues/1345#issuecomment-607490721
        /// <exception cref="T:System.Exception">A <paramref name="clientHandlerConfigurator"/> throws an exception.</exception>
        public HttpClient Build(Action<SocketsHttpHandler> clientHandlerConfigurator = null)
        {
            var clientHandler = new SocketsHttpHandler();
            InitializeClientHandler(clientHandler, out var rootPolicyHandler);

            clientHandlerConfigurator?.Invoke(clientHandler);
            
            var client = ConstructClientWithMiddleware(clientHandler, rootPolicyHandler);

            if(_timeout.HasValue)
                client.Timeout = _timeout.Value;

            return client;
        }

        private void InitializeClientHandler(SocketsHttpHandler clientHandler, out PollyMessageMiddleware rootPolicyHandler)
        {
            rootPolicyHandler = null;

            clientHandler.MaxConnectionsPerServer = Constants.MaxConnectionsPerServer;
            clientHandler.PooledConnectionIdleTimeout = Constants.ConnectionLifeTime;
            clientHandler.PooledConnectionLifetime = Constants.ConnectionLifeTime;

            if (_certificates.Count > 0)
            {
                clientHandler.SslOptions = new SslClientAuthenticationOptions()
                {
                    ClientCertificates = new X509CertificateCollection()
                };

                for (int i = 0; i < _certificates.Count; i++)
                    clientHandler.SslOptions.ClientCertificates.Add(_certificates[i]);
            }


            for (int i = 0; i < _policies.Count; i++)
            {
                if (rootPolicyHandler == null)
                    rootPolicyHandler = new PollyMessageMiddleware(_policies[i], clientHandler);
                else
                {
                    var @new = new PollyMessageMiddleware(_policies[i], rootPolicyHandler);
                    rootPolicyHandler = @new;
                }
            }

        }

#else

        public HttpClient Build(HttpClientHandler clientHandler)
        {
            InitializeClientHandler(clientHandler, out var rootPolicyHandler);
            return ConstructClientWithMiddleware(clientHandler, rootPolicyHandler);
        }

        /// <exception cref="T:System.Exception">A <paramref name="clientHandlerConfigurator"/> throws an exception.</exception>
        public HttpClient Build(Action<HttpClientHandler> clientHandlerConfigurator = null)
        {
            var clientHandler = new HttpClientHandlerEx();

            InitializeClientHandler(clientHandler, out var rootPolicyHandler);

            clientHandlerConfigurator?.Invoke(clientHandler);
            var client = ConstructClientWithMiddleware(clientHandler, rootPolicyHandler);
            
            if (_timeout.HasValue)
                client.Timeout = _timeout.Value;

            return client;
        }

        private void InitializeClientHandler(HttpClientHandler clientHandler, out PollyMessageMiddleware rootPolicyHandler)
        {
            if (_certificates.Count > 0)
            {
                for (int i = 0; i < _certificates.Count; i++)
                    clientHandler.ClientCertificates.Add(_certificates[i]);
            }


            rootPolicyHandler = null;
            for (int i = 0; i < _policies.Count; i++)
            {
                if (rootPolicyHandler == null)
                    rootPolicyHandler = new PollyMessageMiddleware(_policies[i], clientHandler);
                else
                {
                    var @new = new PollyMessageMiddleware(_policies[i], rootPolicyHandler);
                    rootPolicyHandler = @new;
                }
            }
        }
#endif
        private HttpClient ConstructClientWithMiddleware<TClientHandler>(TClientHandler clientHandler, PollyMessageMiddleware rootPolicyHandler)
            where TClientHandler : HttpMessageHandler
        {
            HttpClient client;
            client = CreateClientInternal(clientHandler, rootPolicyHandler, _middlewareHandlers.LastOrDefault());

            InitializeDefaultHeadersIfNeeded();

            if(_timeout.HasValue)
                client.Timeout = _timeout.Value;

            return client;


            void InitializeDefaultHeadersIfNeeded()
            {
                if (_defaultHeaders.Count > 0)
                {
                    foreach (var header in _defaultHeaders)
                    {
                        if (!client.DefaultRequestHeaders.Contains(header.Key))
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
            }
        }

        private HttpClient CreateClientInternal<TClientHandler>(TClientHandler clientHandler,
            PollyMessageMiddleware rootPolicyHandler, DelegatingHandler lastMiddleware) where TClientHandler : HttpMessageHandler
        {
            HttpClient InitializeClientWithPoliciesAndMiddleware()
            {
                HttpClient client;
                if (_middlewareHandlers.Count > 0)
                {
                    if(lastMiddleware == null)
                        throw new InvalidOperationException("One or more middleware handlers is null. This is not supposed to happen!");

                    lastMiddleware.InnerHandler = rootPolicyHandler;
                    client = new HttpClient(_middlewareHandlers.FirstOrDefault(), true);
                }
                else
                    client = new HttpClient(rootPolicyHandler, true);

                return client;
            }

            HttpClient InitializeClientOnlyWithMiddleware()
            {
                lastMiddleware.InnerHandler = clientHandler;
                var client = new HttpClient(_middlewareHandlers.FirstOrDefault(), true);
                return client;
            }

            HttpClient createdClient;
            if (rootPolicyHandler != null)
                createdClient = InitializeClientWithPoliciesAndMiddleware();
            else if (_middlewareHandlers.Count > 0)
                createdClient = InitializeClientOnlyWithMiddleware();
            else
                createdClient = new HttpClient(clientHandler, true);

            if(_baseUrl != null)
                createdClient.BaseAddress = _baseUrl;

            return createdClient;
        }
    }
}
