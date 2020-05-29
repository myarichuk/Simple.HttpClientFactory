[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=myarichuk_Simple.HttpClientFactory&metric=alert_status)](https://sonarcloud.io/dashboard?id=myarichuk_Simple.HttpClientFactory)
[![Nuget](https://img.shields.io/nuget/v/Simple.HttpClientFactory?color=light-green)  ](https://www.nuget.org/packages/Simple.HttpClientFactory/)

### The Why?
A first question would be - why create another factory for ``HttpClient`` if Microsoft have already created an excellent library in ``Microsoft.Http.Extensions``?
(take a look at this [documentation article](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests) to learn more)  
Microsoft's factory works and works well, but it has two drawbacks, in my case those weren't something I could work around.  
First, it is tightly coupled to ``Microsoft.Extensions.DependencyInjection`` package, which in absence of ASP.Net Core or .Net Core hosted service is not viable to use. Yes, I could initialize the dependency injection and configure it, but the resulting code was ugly and not elegant.
  
Second issue is package versions. I was unable to use Microsoft's ``HttpClient`` factory in projects that depended on an old version of ``Microsoft.Http.Extensions`` - in those projects I couldn't change the dependency versions, so unfortunately it was a no-go.  

### The What
The ``HttpClientFactory`` is lightweight, with minimal dependencies and properly handles a well-known issue of ``HttpClient`` - respecting DNS changes.  
(if you are unsure what do I mean by this, there is a [really awesome blogpost](https://www.nimaara.com/beware-of-the-net-httpclient/), it explains the issue in-depth)  
Also, the factory incorporates support for [Polly](https://www.hanselman.com/blog/AddingResilienceAndTransientFaultHandlingToYourNETCoreHttpClientWithPolly.aspx) policies, as can be seen in the code sample below.

### The How
Using the client factory is simple, and pretty self-explanatory. Here is how ``HttpClient`` that supports HTTPS and has a [retry policy](https://www.c-sharpcorner.com/article/using-retry-pattern-in-asp-net-core-via-polly/) on transient exceptions.

```cs
public HttpClient CreateClient() =>
    HttpClientFactory
        .Create()
        .WithCertificate(DefaultDevCert.Get()) //configure with one or more X509Certificate2 instances
        .WithPolicy(HttpPolicyExtensions //Polly error policy
                        .HandleTransientHttpError() // add retry on HttpRequestException with 5XX and 408 codes
                        .RetryAsync(3))
        .Build();

```
