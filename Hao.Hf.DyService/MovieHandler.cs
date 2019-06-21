using Hao.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hao.Hf.DyService
{
    public partial class DyService
    {
        private async Task<Movie> FillMovieInfoFormWeb(string onlineURL)
        {
            try
            {
                var movieHTML = await _http.GetHtmlByUrl(onlineURL);
                if (string.IsNullOrWhiteSpace(movieHTML)) return null;
                var movieDoc = htmlParser.ParseDocument(movieHTML);

                var score = movieDoc.QuerySelector("strong.rank").InnerHtml;
                //电影的详细介绍 在id为Zoom的标签中
                var zoom = movieDoc.GetElementById("Zoom");

                var ps = zoom.QuerySelectorAll("p").ToList();

                var divs = zoom.QuerySelectorAll("div").ToList();
                if (divs.Count > 5)
                {
                    ps = ps.Take(2).ToList();
                    ps.AddRange(divs);
                }

                var lstDownLoadURL = movieDoc.QuerySelectorAll("td > a").Where(a => !a.GetAttribute("href").Contains(".html")).Select(a => a.InnerHtml).ToList();

                int dIndex = 0;
                while (!ps[dIndex].InnerHtml.Contains("◎主　　演"))
                {
                    dIndex++;
                }

                int cIndex = 0;
                while (!ps[cIndex].InnerHtml.Contains("◎简　　介"))
                {
                    cIndex++;
                }

                #region HtmlString

                var zoomHtml = movieDoc.GetElementById("Zoom").InnerHtml;

                string[] splitFeature = new string[] { "</" };

                var area = "";
                if (zoomHtml.Contains("◎产　　地"))
                {
                    area = zoomHtml.Split(new string[] { "◎产　　地" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();
                }
                else if (zoomHtml.Contains("◎国　　家"))
                {
                    area = zoomHtml.Split(new string[] { "◎国　　家" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();
                }

                var nameother = zoomHtml.Split(new string[] { "◎译　　名" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();

                var types = zoomHtml.Split(new string[] { "◎类　　别" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();


                var yearStr = zoomHtml.Split(new string[] { "◎年　　代" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();
                var year = HConvert.ToInt(yearStr.Substring(0,4));

                var director = zoomHtml.Split(new string[] { "◎导　　演" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();

                var releaseDate = zoomHtml.Split(new string[] { "◎上映日期" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();
                if (!string.IsNullOrWhiteSpace(releaseDate))
                {
                    foreach (Match match in Regex.Matches(releaseDate, @"\d{4}-\d{1,2}"))
                    {
                        releaseDate = match.Groups[0].Value;
                    }
                    if (releaseDate.Length == 4) 
                    {
                        releaseDate = year.ToString() + "-01-01";
                    }
                }
                else
                {
                    releaseDate = year.ToString() + "-01-01";
                }
                #endregion

                var movieInfo = new Movie()
                {
                    Name = movieDoc.QuerySelectorAll("div.title_all > h1").FirstOrDefault().InnerHtml,
                    NameAnother = nameother,
                    Year = year,
                    Area = area,
                    Types = await ConvertTypes(types.Split('/')),
                    ReleaseDate = HConvert.ToDateTime(releaseDate),
                    Score = HConvert.ToFloat(score),
                    Director = director,
                    MainActors = $",{ps[dIndex].InnerHtml.Substring(6)},{ps[dIndex + 2].InnerHtml.Substring(6)},{ps[dIndex + 3].InnerHtml.Substring(6)},{ps[dIndex + 4].InnerHtml.Substring(6)},{ps[dIndex + 5].InnerHtml.Substring(6)},",
                    CoverPicture = ps[0].Children.FirstOrDefault().GetAttribute("src").Trim(),
                    Description = ps[cIndex + 1].InnerHtml.Replace("&nbsp;", "").Trim(),
                    DownloadUrlFirst = lstDownLoadURL?.FirstOrDefault(),
                    DownloadUrlSecond = lstDownLoadURL.Count() > 2 && !string.IsNullOrWhiteSpace(lstDownLoadURL[1]) ? lstDownLoadURL[1].Trim() : "",
                    DownloadUrlThird = lstDownLoadURL.Count() > 3 && !string.IsNullOrWhiteSpace(lstDownLoadURL[2]) ? lstDownLoadURL[2].Trim() : "",
                };
                return movieInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
