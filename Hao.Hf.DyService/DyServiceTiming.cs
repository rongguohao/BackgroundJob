using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hao.Hf.DyService
{
    public partial class DyService 
    {
        /// <summary>
        /// 定时拉取电影方法
        /// </summary>
        /// <returns></returns>
        public async Task PullMovieTiming()
        {
            //通过URL获取HTML
            var htmlDoc = await _http.GetHtmlByUrl("http://www.dy2018.com/");
            //HTML 解析成 IDocument
            var dom = htmlParser.ParseDocument(htmlDoc);
            var divInfo = dom.QuerySelectorAll("div.co_area2")[1].QuerySelector("div.co_content222");

            if (divInfo != null)
            {
                var hrefs = divInfo.QuerySelectorAll("a").Where(a => a.GetAttribute("href").Contains("/i/")).ToList();

                foreach (var a in hrefs)
                {
                    //拼接成完整链接
                    var onlineURL = "http://www.dy2018.com" + a.GetAttribute("href");

                    Movie movieInfo = await FillMovieInfoFormWeb(onlineURL);
                    if (movieInfo == null) continue;
                    var success = await InsertDB(movieInfo);
                }
            }
        }
    }
}
