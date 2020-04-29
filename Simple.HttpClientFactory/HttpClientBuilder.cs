using Polly;
using Simple.HttpClientFactory.Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Simple.HttpClientFactory
{
    public class HttpClientBuilder : IHttpClientBuilder
    {        
        private TimeSpan? _connectionTimeout;
        private readonly List<X509Certificate2> _certificates = new List<X509Certificate2>();
        private readonly List<IAsyncPolicy<HttpResponseMessage>> _policies = new List<IAsyncPolicy<HttpResponseMessage>>();

        public IHttpClientBuilder WithCertificate(params X509Certificate2[] certificates)
        {                        
            _certificates.AddRange(certificates);
            return this;
        }

        public IHttpClientBuilder WithPolicy(IAsyncPolicy<HttpResponseMessage> policy)
        {
            _policies.Add(policy);
            return this;
        }

        public IHttpClientBuilder WithConnectionTimeout(in TimeSpan connectionTimeout)
        {
            _connectionTimeout = connectionTimeout;
            return this;
        }        


        public HttpClient Build(Action<HttpClientHandler> clientHandlerConfigurator = null)
        {
            PolicyHttpMessageHandler policyHandler = null;
            
            var clientHandler = new HttpClientHandlerEx();
            
            if(_certificates.Count > 0)
                clientHandler.ClientCertificates.AddRange(_certificates.ToArray());
            
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

            return new HttpClient(policyHandler ?? (HttpMessageHandler)clientHandler, true);
        }

    }
}
