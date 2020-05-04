using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.HttpClientFactory.MessageHandlers
{
    public class ExceptionTranslatorRequestMiddleware : DelegatingHandler
    {
        private readonly Func<HttpRequestException, bool> _exceptionHandlingPredicate;
        private readonly Func<HttpRequestException, Exception> _exceptionHandler;

        public event EventHandler<HttpRequestException> RequestException;
        public event EventHandler<Exception> TransformedRequestException;

        public ExceptionTranslatorRequestMiddleware(
            Func<HttpRequestException, bool> exceptionHandlingPredicate,
            Func<HttpRequestException, Exception> exceptionHandler, DelegatingHandler handler)  : base(handler)
        {
            _exceptionHandlingPredicate = exceptionHandlingPredicate ?? throw new ArgumentNullException(nameof(exceptionHandlingPredicate));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch(HttpRequestException e)
            {
                RequestException?.Invoke(this, e);

                if(!_exceptionHandlingPredicate(e))
                {
                    response?.Content?.Dispose();
                    throw;
                }

                var transformed = _exceptionHandler(e);
                if(transformed != null)
                {
                    response?.Content?.Dispose();
                    TransformedRequestException?.Invoke(this, transformed);
                    throw transformed;
                }
            }

            return response;
        }
    }
}
