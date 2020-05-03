using Polly;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.HttpClientFactory.Polly
{
   public class PolicyHttpMessageHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        /// <summary>
        /// Creates a new <see cref="PolicyHttpMessageHandler"/>.
        /// </summary>
        /// <param name="policy">The policy.</param>
        /// <param name="inner">The inner handler which is responsible for processing the HTTP response messages.</param>
        public PolicyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy, HttpMessageHandler inner) : base(inner) => 
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));

        /// <summary>
        /// Creates a new <see cref="PolicyHttpMessageHandler"/>.
        /// </summary>
        /// <param name="policy">The policy.</param>
        public PolicyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy) => 
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));


        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return SendAsyncInternal(request, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsyncInternal(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Guarantee the existence of a context for every policy execution, but only create a new one if needed. This
            // allows later handlers to flow state if desired.
            var cleanUpContext = false;
            if (!request.TryGetPolicyExecutionContext(out var context))
            {
                context = new Context();
                request.SetPolicyExecutionContext(context);
                cleanUpContext = true;
            }

            HttpResponseMessage response;
            try
            {
                response = await _policy.ExecuteAsync(
                    async (c, ct) => await base.SendAsync(request, cancellationToken),
                        context, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (cleanUpContext)
                    request.SetPolicyExecutionContext(null);
            }

            return response;
        }
    }
}
