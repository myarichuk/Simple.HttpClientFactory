using Polly;
using Simple.HttpClientFactory.Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
#if NETCOREAPP2_1
using System.Net.Security;
#endif
using System.Security.Cryptography.X509Certificates;

namespace Simple.HttpClientFactory
{
    internal class HttpClientBuilder : IHttpClientBuilder
    {        
        private TimeSpan? _connectionTimeout;
        private readonly List<X509Certificate2> _certificates = new List<X509Certificate2>();
        private readonly List<IAsyncPolicy<HttpResponseMessage>> _policies = new List<IAsyncPolicy<HttpResponseMessage>>();

        #if NETCOREAPP2_1
        private const int MAX_CONNECTION_PER_SERVER = 20;
        private static readonly TimeSpan ConnectionLifeTime = TimeSpan.FromMinutes(1);
        #endif


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


        #if NETCOREAPP2_1
        
        //ServicePointManager in .Net Core is a no-op so we need to do this
        //see https://github.com/dotnet/extensions/issues/1345#issuecomment-607490721
        public HttpClient Build(Action<SocketsHttpHandler> clientHandlerConfigurator = null)
        {
            PolicyHttpMessageHandler policyHandler = null;
            
            var clientHandler = new SocketsHttpHandler
            {
                // https://github.com/dotnet/corefx/issues/26895
                PooledConnectionLifetime = ConnectionLifeTime,
                PooledConnectionIdleTimeout = ConnectionLifeTime,
                MaxConnectionsPerServer = MAX_CONNECTION_PER_SERVER
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

            return new HttpClient(policyHandler ?? (HttpMessageHandler)clientHandler, true);
        }

        #else

        public HttpClient Build(Action<HttpClientHandler> clientHandlerConfigurator = null)
        {
            PolicyHttpMessageHandler policyHandler = null;
            
            var clientHandler = new HttpClientHandlerEx();
            
            if(_certificates.Count > 0)
            {
                clientHandler.ClientCertificates.AddRange(_certificates.ToArray());
                
                #if NET472

                clientHandler.ClientCertificateOptions = ClientCertificateOption.Automatic;

                #endif
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

            return new HttpClient(policyHandler ?? (HttpMessageHandler)clientHandler, true);
        }

        #endif
    }
}
