using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hao.Hf.DyService
{
    public interface IHttpHelper
    {
        Task<string> GetHtmlByUrl(string url);
    }
}
