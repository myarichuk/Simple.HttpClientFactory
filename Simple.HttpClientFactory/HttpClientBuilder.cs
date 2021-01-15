using Simple.HttpClientFactory.MessageHandlers;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
#if NETCOREAPP2_1
using System.Net.Security;
#endif
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace Simple.HttpClientFactory
{
    internal class HttpClientBuilder : IHttpClientBuilder
    {        
        private Uri _baseUrl;
        private readonly Dictionary<string, string> _defaultHeaders = new Dictionary<string, string>();
        private readonly List<X509Certificate2> _certificates = new List<X509Certificate2>();
        private readonly List<IAsyncPolicy<HttpResponseMessage>> _policies = new List<IAsyncPolicy<HttpResponseMessage>>();
        private TimeSpan? _timeout;
        private readonly List<DelegatingHandler> _middlewareHandlers = new List<DelegatingHandler>();
#if NETCOREAPP2_1
        private Action<SocketsHttpHandler> _primaryMessageHandlerConfigurator;
#else
        private Action<HttpClientHandler> _primaryMessageHandlerConfigurator;
#endif

        public IHttpClientBuilder WithBaseUrl(string baseUrl)
        {
            return WithBaseUrl(new Uri(baseUrl));
        }

        public IHttpClientBuilder WithBaseUrl(Uri baseUrl)
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IHttpClientBuilder WithDefaultHeader(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (!_defaultHeaders.ContainsKey(name))
                _defaultHeaders.Add(name, value);

            return this;
        }

        public IHttpClientBuilder WithDefaultHeaders(IDictionary<string, string> headers)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));

            foreach(var kvp in headers)
                WithDefaultHeader(kvp.Key, kvp.Value);

            return this;
        }

        private void WithCertificate(X509Certificate2 certificate)
        {
            if (certificate == null) throw new ArgumentNullException(nameof(certificate));
            
            _certificates.Add(certificate);
        }

        public IHttpClientBuilder WithCertificate(params X509Certificate2[] certificate)
        {
            return WithCertificates(certificate);
        }

        public IHttpClientBuilder WithCertificates(IEnumerable<X509Certificate2> certificates)
        {
            if (certificates == null) throw new ArgumentNullException(nameof(certificates));

            var certificateList = certificates.ToList();

            if (!certificateList.Any()) throw new ArgumentException("The provided collection must contain at least one certificate", nameof(certificates));

            foreach (var certificate in certificateList)
                WithCertificate(certificate);

            return this;
        }

        private void WithPolicy(IAsyncPolicy<HttpResponseMessage> policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            _policies.Add(policy);
        }

        public IHttpClientBuilder WithPolicy(params IAsyncPolicy<HttpResponseMessage>[] policy)
        {
            return WithPolicies(policy);
        }

        public IHttpClientBuilder WithPolicies(IEnumerable<IAsyncPolicy<HttpResponseMessage>> policies)
        {
            if (policies == null) throw new ArgumentNullException(nameof(policies));

            var policyList = policies.ToList();

            if (!policyList.Any()) throw new ArgumentException("The provided collection must contain at least one policy", nameof(policies));

            foreach (var policy in policyList)
                WithPolicy(policy);

            return this;
        }

        public IHttpClientBuilder WithTimeout(in TimeSpan timeout)
        {
            _timeout = timeout;

            return this;
        }

        private IHttpClientBuilder WithMessageHandler(DelegatingHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (_middlewareHandlers.Count > 0) _middlewareHandlers.Last().InnerHandler = handler;

            _middlewareHandlers.Add(handler);

            return this;
        }

        public IHttpClientBuilder WithMessageHandler(params DelegatingHandler[] handler)
        {
            return WithMessageHandlers(handler);
        }

        public IHttpClientBuilder WithMessageHandlers(IEnumerable<DelegatingHandler> handlers)
        {
            if (handlers == null) throw new ArgumentNullException(nameof(handlers));

            var handlerList = handlers.ToList();

            if (!handlerList.Any()) throw new ArgumentException("The provided collection must contain at least one message handler", nameof(handlers));

            foreach (var handler in handlerList)
                WithMessageHandler(handler);

            return this;
        }

        public IHttpClientBuilder WithMessageExceptionHandler(Func<HttpRequestException, bool> exceptionHandlingPredicate,
                                                              Func<HttpRequestException, Exception> exceptionHandler,
                                                              EventHandler<HttpRequestException> requestExceptionEventHandler = null,
                                                              EventHandler<Exception> transformedRequestExceptionEventHandler = null) => WithMessageHandler(new ExceptionTranslatorRequestMiddleware(exceptionHandlingPredicate, exceptionHandler, requestExceptionEventHandler, transformedRequestExceptionEventHandler));

#if NETCOREAPP2_1

        public IHttpClientBuilder WithPrimaryMessageHandlerConfigurator(Action<SocketsHttpHandler> configurator)
        {
            _primaryMessageHandlerConfigurator = configurator ?? throw new ArgumentNullException(nameof(configurator));

            return this;
        }

        public HttpClient Build(SocketsHttpHandler defaultPrimaryMessageHandler)
        {
            if (defaultPrimaryMessageHandler == null) throw new ArgumentNullException(nameof(defaultPrimaryMessageHandler));

            InitializePrimaryMessageHandler(defaultPrimaryMessageHandler, out var rootPolicyHandler);

            return ConstructClientWithMiddleware(defaultPrimaryMessageHandler, rootPolicyHandler);
        }

        //ServicePointManager in .Net Core is a no-op so we need to do this
        //see https://github.com/dotnet/extensions/issues/1345#issuecomment-607490721
        public HttpClient Build(Action<SocketsHttpHandler> primaryMessageHandlerConfigurator = null)
        {
            var primaryMessageHandler = new SocketsHttpHandler();
            InitializePrimaryMessageHandler(primaryMessageHandler, out var rootPolicyHandler);

            primaryMessageHandlerConfigurator?.Invoke(primaryMessageHandler);
            
            var client = ConstructClientWithMiddleware(primaryMessageHandler, rootPolicyHandler);

            if (_timeout.HasValue)
                client.Timeout = _timeout.Value;

            return client;
        }

        private void InitializePrimaryMessageHandler(SocketsHttpHandler primaryMessageHandler, out PollyMessageMiddleware rootPolicyHandler)
        {
            rootPolicyHandler = null;

            primaryMessageHandler.MaxConnectionsPerServer = Constants.MaxConnectionsPerServer;
            primaryMessageHandler.PooledConnectionIdleTimeout = Constants.ConnectionLifeTime;
            primaryMessageHandler.PooledConnectionLifetime = Constants.ConnectionLifeTime;

            if (_certificates.Count > 0)
            {
                primaryMessageHandler.SslOptions = new SslClientAuthenticationOptions()
                {
                    ClientCertificates = new X509CertificateCollection()
                };

                for (int i = 0; i < _certificates.Count; i++)
                    primaryMessageHandler.SslOptions.ClientCertificates.Add(_certificates[i]);
            }

            for (int i = 0; i < _policies.Count; i++)
            {
                if (rootPolicyHandler == null)
                    rootPolicyHandler = new PollyMessageMiddleware(_policies[i], primaryMessageHandler);
                else
                {
                    var @new = new PollyMessageMiddleware(_policies[i], rootPolicyHandler);
                    rootPolicyHandler = @new;
                }
            }

            _primaryMessageHandlerConfigurator?.Invoke(primaryMessageHandler);
        }

#else

        public IHttpClientBuilder WithPrimaryMessageHandlerConfigurator(Action<HttpClientHandler> configurator)
        {
            _primaryMessageHandlerConfigurator = configurator ?? throw new ArgumentNullException(nameof(configurator));

            return this;
        }

        public HttpClient Build(HttpClientHandler defaultPrimaryMessageHandler)
        {
            if (defaultPrimaryMessageHandler == null) throw new ArgumentNullException(nameof(defaultPrimaryMessageHandler));

            InitializePrimaryMessageHandler(defaultPrimaryMessageHandler, out var rootPolicyHandler);

            return ConstructClientWithMiddleware(defaultPrimaryMessageHandler, rootPolicyHandler);
        }

        public HttpClient Build(Action<HttpClientHandler> configurator = null)
        {
            var primaryMessageHandler = new HttpClientHandlerEx();

            InitializePrimaryMessageHandler(primaryMessageHandler, out var rootPolicyHandler);

            configurator?.Invoke(primaryMessageHandler);
            var client = ConstructClientWithMiddleware(primaryMessageHandler, rootPolicyHandler);
            
            if (_timeout.HasValue)
                client.Timeout = _timeout.Value;

            return client;
        }

        private void InitializePrimaryMessageHandler(HttpClientHandler primaryMessageHandler, out PollyMessageMiddleware rootPolicyHandler)
        {
            if (_certificates.Count > 0)
            {
                for (int i = 0; i < _certificates.Count; i++)
                    primaryMessageHandler.ClientCertificates.Add(_certificates[i]);
            }


            rootPolicyHandler = null;
            for (int i = 0; i < _policies.Count; i++)
            {
                if (rootPolicyHandler == null)
                    rootPolicyHandler = new PollyMessageMiddleware(_policies[i], primaryMessageHandler);
                else
                {
                    var @new = new PollyMessageMiddleware(_policies[i], rootPolicyHandler);

                    rootPolicyHandler = @new;
                }
            }

            _primaryMessageHandlerConfigurator?.Invoke(primaryMessageHandler);
        }
#endif
        private HttpClient ConstructClientWithMiddleware<TPrimaryMessageHandler>(TPrimaryMessageHandler primaryMessageHandler, PollyMessageMiddleware rootPolicyHandler)
            where TPrimaryMessageHandler : HttpMessageHandler
        {
            var client = CreateClientInternal(primaryMessageHandler, rootPolicyHandler, _middlewareHandlers.LastOrDefault());

            InitializeDefaultHeadersIfNeeded();

            if (_timeout.HasValue) client.Timeout = _timeout.Value;

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

        private HttpClient CreateClientInternal<TPrimaryMessageHandler>(TPrimaryMessageHandler primaryMessageHandler, PollyMessageMiddleware rootPolicyHandler, DelegatingHandler lastMiddleware)
            where TPrimaryMessageHandler : HttpMessageHandler
        {
            HttpClient createdClient;
            if (rootPolicyHandler != null)
                createdClient = InitializeClientWithPoliciesAndMiddleware();
            else if (_middlewareHandlers.Count > 0)
                createdClient = InitializeClientOnlyWithMiddleware();
            else
                createdClient = new HttpClient(primaryMessageHandler, true);

            if (_baseUrl != null)
                createdClient.BaseAddress = _baseUrl;

            return createdClient;


            HttpClient InitializeClientWithPoliciesAndMiddleware()
            {
                HttpClient client;

                if (_middlewareHandlers.Count > 0)
                {
                    lastMiddleware.InnerHandler = rootPolicyHandler;
                    client = new HttpClient(_middlewareHandlers.FirstOrDefault(), true);
                }
                else
                    client = new HttpClient(rootPolicyHandler, true);

                return client;
            }

            HttpClient InitializeClientOnlyWithMiddleware()
            {
                lastMiddleware.InnerHandler = primaryMessageHandler;
                var client = new HttpClient(_middlewareHandlers.FirstOrDefault(), true);

                return client;
            }
        }
    }
}
