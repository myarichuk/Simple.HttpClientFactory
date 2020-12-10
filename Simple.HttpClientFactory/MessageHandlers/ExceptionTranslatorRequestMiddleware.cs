using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.HttpClientFactory.MessageHandlers
{
    public sealed class ExceptionTranslatorRequestMiddleware : DelegatingHandler
    {
        private readonly Func<HttpRequestException, bool> _exceptionHandlingPredicate;
        private readonly Func<HttpRequestException, Exception> _exceptionHandler;

        public event EventHandler<HttpRequestException> RequestException;
        public event EventHandler<Exception> TransformedRequestException;

        internal ExceptionTranslatorRequestMiddleware(Func<HttpRequestException, bool> exceptionHandlingPredicate, Func<HttpRequestException, Exception> exceptionHandler, EventHandler<HttpRequestException> requestExceptionEventHandler = null, EventHandler<Exception> transformedRequestExceptionEventHandler = null) 
        {
            _exceptionHandlingPredicate = exceptionHandlingPredicate ?? throw new ArgumentNullException(nameof(exceptionHandlingPredicate));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            RequestException += requestExceptionEventHandler;
            TransformedRequestException += transformedRequestExceptionEventHandler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                return response;
            }
            catch (HttpRequestException e)
            {
                RequestException?.Invoke(this, e);

                response?.Content.Dispose();

                if (!_exceptionHandlingPredicate(e))
                {
                    throw;
                }

                var transformed = _exceptionHandler(e);
                if (transformed != null)
                {
                    TransformedRequestException?.Invoke(this, transformed);
                    throw transformed;
                }

                throw new Exception("Request exception transformation function cannot return null", e);
            }
        }
    }
}
