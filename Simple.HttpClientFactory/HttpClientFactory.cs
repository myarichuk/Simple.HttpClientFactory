using Easy.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Simple.HttpClientFactory
{
    //credit: some code is adapted from https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/RestClient.cs
    public class HttpClientFactory
    {
        public IHttpClientBuilder Create() => new HttpClientBuilder();
    }
}
