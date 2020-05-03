using Polly;
using Simple.HttpClientFactory.Polly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    //some sanity checks
    public class PollyHttpMessageHandlerTests
    {
        [Fact]
        public void Ctor_with_null_should_throw() => 
            Assert.Throws<ArgumentNullException>(() => new PolicyHttpMessageHandler(null));

        [Fact]
        public async Task Null_param_in_send_async_should_throw() 
        {
            var middlewareHandler = new PolicyHttpMessageHandler(Policy<HttpResponseMessage>
                            .Handle<HttpRequestException>()
                            .OrResult(result => (int)result.StatusCode >= 500 || result.StatusCode == HttpStatusCode.RequestTimeout)
                            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)));

            using(var client = HttpClientFactory.Create(middlewareHandler).Build())
                await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAsync(null));
        }
    }
}
