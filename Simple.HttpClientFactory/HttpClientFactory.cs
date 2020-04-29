namespace Simple.HttpClientFactory
{
    //credit: some code is adapted from https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/RestClient.cs
    public static class HttpClientFactory
    {
        public static IHttpClientBuilder Create() => new HttpClientBuilder();
    }
}
