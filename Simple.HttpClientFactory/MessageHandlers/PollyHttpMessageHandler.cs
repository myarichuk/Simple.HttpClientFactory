using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Simple.HttpClientFactory.MessageHandlers
{
   public class PollyMessageMiddleware : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        /// <summary>
        /// Creates a new <see cref="PollyMessageMiddleware"/>.
        /// </summary>
        /// <param name="policy">The policy.</param>
        /// <param name="inner">The inner handler which is responsible for processing the HTTP response messages.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="policy"/> is <see langword="null"/></exception>
        public PollyMessageMiddleware(IAsyncPolicy<HttpResponseMessage> policy, HttpMessageHandler inner) : base(inner) => 
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));

        /// <summary>
        /// Creates a new <see cref="PollyMessageMiddleware"/>.
        /// </summary>
        /// <param name="policy">The policy.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="policy"/> is <see langword="null"/></exception>
        public PollyMessageMiddleware(IAsyncPolicy<HttpResponseMessage> policy) => 
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));


        /// <inheritdoc />
        /// <exception cref="T:System.ArgumentNullException"><paramref name="request"/> is <see langword="null"/></exception>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return SendAsyncInternal(request, cancellationToken);
        }

        private Task<HttpResponseMessage> SendAsyncInternal(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Guarantee the existence of a context for every policy execution, but only create a new one if needed. This
            // allows later handlers to flow state if desired.
            var cleanUpContext = false;
            var context = GetOrCreatePolicyExecutionContext(request, ref cleanUpContext);

            return _policy.ExecuteAsync(
                async (c, ct) => await base.SendAsync(request, cancellationToken), context, cancellationToken)
                .ContinueWith(t =>
                {
                    if(cleanUpContext)
                        request.SetPolicyExecutionContext(null);
                    return t.Result;
                }, cancellationToken);

            Context GetOrCreatePolicyExecutionContext(HttpRequestMessage httpRequestMessage, ref bool b)
            {
                if (!httpRequestMessage.TryGetPolicyExecutionContext(out var fetchedContext))
                {
                    fetchedContext = new Context();
                    httpRequestMessage.SetPolicyExecutionContext(fetchedContext);
                    b = true;
                }

                return fetchedContext;
            }
        }
    }
}
