using Polly;
using Simple.HttpClientFactory.Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
#if NETCOREAPP2_1
using System.Net.Security;
#endif
using System.Security.Cryptography.X509Certificates;

namespace Simple.HttpClientFactory
{
    internal class HttpClientBuilder : IHttpClientBuilder
    {        
        private TimeSpan? _timeout;
        private readonly List<X509Certificate2> _certificates = new List<X509Certificate2>();
        private readonly List<IAsyncPolicy<HttpResponseMessage>> _policies = new List<IAsyncPolicy<HttpResponseMessage>>();
        private readonly List<DelegatingHandler> _middlewareHandlers = new List<DelegatingHandler>();
        private readonly Dictionary<string, string> _defaultHeaders = new Dictionary<string, string>();

        public IHttpClientBuilder WithDefaultHeaders(IReadOnlyDictionary<string, string> headers)
        {
            foreach(var kvp in headers)
            {
                if(!_defaultHeaders.ContainsKey(kvp.Key))
                    _defaultHeaders.Add(kvp.Key, kvp.Value);
            }
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
            WithMessageHandler(new MessageExceptionHandler(exceptionHandlingPredicate, exceptionHandler, null));

        public IHttpClientBuilder WithMessageHandler(DelegatingHandler handler)
        {
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
        
        //ServicePointManager in .Net Core is a no-op so we need to do this
        //see https://github.com/dotnet/extensions/issues/1345#issuecomment-607490721
        public HttpClient Build(Action<SocketsHttpHandler> clientHandlerConfigurator = null)
        {
            PolicyHttpMessageHandler policyHandler = null;
            
            var clientHandler = new SocketsHttpHandler
            {
                // https://github.com/dotnet/corefx/issues/26895
                PooledConnectionLifetime = Constants.ConnectionLifeTime,
                PooledConnectionIdleTimeout = Constants.ConnectionLifeTime,
                MaxConnectionsPerServer = Constants.MaxConnectionsPerServer
            };
            
            if(_certificates.Count > 0)
            {
                clientHandler.SslOptions = new SslClientAuthenticationOptions()
                {
                    ClientCertificates = new X509CertificateCollection()
                };

                for(int i = 0; i < _certificates.Count; i++)
                    clientHandler.SslOptions.ClientCertificates.Add(_certificates[i]);
            }

            clientHandlerConfigurator?.Invoke(clientHandler);

            for(int i = 0; i < _policies.Count; i++)
            {
                if(policyHandler == null)
                    policyHandler = new PolicyHttpMessageHandler(_policies[i], clientHandler);
                else
                {
                    var @new = new PolicyHttpMessageHandler(_policies[i], policyHandler);
                    policyHandler = @new;
                }
            }       

            var client = ConstructClientWithMiddleware(clientHandler, policyHandler);

            if(_timeout.HasValue)
                client.Timeout = _timeout.Value;

            return client;
        }

        #else

        public HttpClient Build(Action<HttpClientHandler> clientHandlerConfigurator = null)
        {

            var clientHandler = new HttpClientHandlerEx();

            if (_certificates.Count > 0)
                clientHandler.ClientCertificates.AddRange(_certificates.ToArray());

            clientHandlerConfigurator?.Invoke(clientHandler);

            PolicyHttpMessageHandler policyHandler = null;
            for (int i = 0; i < _policies.Count; i++)
            {
                if (policyHandler == null)
                    policyHandler = new PolicyHttpMessageHandler(_policies[i], clientHandler);
                else
                {
                    var @new = new PolicyHttpMessageHandler(_policies[i], policyHandler);
                    policyHandler = @new;
                }
            }

            var client = ConstructClientWithMiddleware(clientHandler, policyHandler);
            
            if (_timeout.HasValue)
                client.Timeout = _timeout.Value;

            return client;
        }
#endif
        private HttpClient ConstructClientWithMiddleware<TClientHandler>(TClientHandler clientHandler, PolicyHttpMessageHandler policyHandler)
            where TClientHandler : HttpMessageHandler
        {
            HttpClient client;
            if (policyHandler != null)
            {
                if (_middlewareHandlers.Count > 0)
                {
                    _middlewareHandlers.LastOrDefault().InnerHandler = policyHandler;
                    client = new HttpClient(_middlewareHandlers.FirstOrDefault(), true);
                }
                else
                    client = new HttpClient(policyHandler, true);
            }
            else if (_middlewareHandlers.Count > 0)
            {
                _middlewareHandlers.LastOrDefault().InnerHandler = clientHandler;
                client = new HttpClient(_middlewareHandlers.FirstOrDefault(), true);
            }
            else
                client = new HttpClient(clientHandler, true);

            if(_defaultHeaders.Count > 0)
            {
                foreach(var header in _defaultHeaders)
                {
                    if(!client.DefaultRequestHeaders.Contains(header.Key))
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            return client;
        }
    }
}
