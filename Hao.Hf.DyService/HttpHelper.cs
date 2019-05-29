using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Hao.Hf.DyService
{
    public class HttpHelper : IHttpHelper
    {
        public IHttpClientFactory _httpClient;

        public HttpHelper(IHttpClientFactory httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetHtmlByUrl(string url)
        {
            try
            {
                return await GetHtml(url);
            }
            catch (TaskCanceledException ex)
            {
                return await GetHtml(url);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("http异常:" + ex.ToString());
                return string.Empty;
            }
        }


        private async Task<string> GetHtml(string url)
        {
            var client = _httpClient.CreateClient("dy");
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var t = await response.Content.ReadAsByteArrayAsync();
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var ret = System.Text.Encoding.GetEncoding("GB2312").GetString(t);
                return ret;
            }
            return string.Empty;
        }
    }
}
