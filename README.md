![Tests](https://github.com/myarichuk/Simple.HttpClientFactory/workflows/Tests/badge.svg)
### The Why?
A first question would be - why create another factory for ``HttpClient`` if Microsoft have already created an excellent library in ``Microsoft.Http.Extensions``  
(take a look at this [documentation article](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests) to learn more)  
Microsoft's factory works and works well, but it has two drawbacks, that in my case weren't something I could work around.  
First, it is tightly couple to ``Microsoft.Extensions.DependencyInjection`` package, which in absence of ASP.Net Core or .Net Core hosted service is not viable to use.  
  
Second issue is package versions. I was unable to use Microsoft's ``HttpClient`` factory in projects that depended on an old version of ``Microsoft.Http.Extensions`` - in those projects I was not able to change the dependency versions, so unfortunately it was a no-go.  

### The What
The ``HttpClientFactory`` is lightweight, with minimal dependencies and properly handles an well-known issue of ``HttpClient`` - respecting DNS changes.  
(if you are unsure what do I mean by this, there is a [really awesome blogpost](https://www.nimaara.com/beware-of-the-net-httpclient/), it explains the issue in-depth)  
Also, the factory incorporates support for [Polly](https://www.hanselman.com/blog/AddingResilienceAndTransientFaultHandlingToYourNETCoreHttpClientWithPolly.aspx) policies, as can be seen in the code sample below.

### The How
Using the client factory is simple, and pretty self-explanatory. Here is how ``HttpClient`` that supports HTTPS and has a [retry policy](https://www.c-sharpcorner.com/article/using-retry-pattern-in-asp-net-core-via-polly/) on transient exceptions.

```cs
public HttpClient CreateClient() =>
    HttpClientFactory
        .Create()
        .WithCertificate(DefaultDevCert.Get())
        .WithPolicy(HttpPolicyExtensions
                        .HandleTransientHttpError() // HttpRequestException, 5XX and 408
                        .RetryAsync(3))
        .Build();

```
