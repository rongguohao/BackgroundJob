﻿using Hao.Utility;
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

                var score = movieDoc.QuerySelector("strong.rank").InnerHtml.TrimAll();
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
                    area = zoomHtml.Split(new string[] { "◎产　　地" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                }
                else if (zoomHtml.Contains("◎国　　家"))
                {
                    area = zoomHtml.Split(new string[] { "◎国　　家" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                }
                else if (zoomHtml.Contains("◎地　　区"))
                {
                    area = zoomHtml.Split(new string[] { "◎地　　区" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                }

                var nameother = zoomHtml.Split(new string[] { "◎译　　名" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();

                var types = zoomHtml.Split(new string[] { "◎类　　别" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();


                var yearStr = zoomHtml.Split(new string[] { "◎年　　代" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                var year = HConvert.ToInt(yearStr.Substring(0,4));

                var director = zoomHtml.Split(new string[] { "◎导　　演" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();

                var releaseDate = zoomHtml.Split(new string[] { "◎上映日期" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
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

                var name = movieDoc.QuerySelectorAll("div.title_all > h1").FirstOrDefault().InnerHtml.TrimAll();
                List<string> matchValue = new List<string>();
                foreach (Match m in Regex.Matches(name, "(?<=《)[^》]+(?=》)"))
                {
                    matchValue.Add(m.Value);
                }
                if (matchValue.Count > 0)
                {
                    name = matchValue.FirstOrDefault();
                }
                #endregion

                var movieInfo = new Movie()
                {
                    Name = name,
                    NameAnother = nameother,
                    Year = year,
                    ReleaseDate = HConvert.ToDateTime(releaseDate),
                    Score = HConvert.ToFloat(score),
                    Director = director,
                    MainActors = $",{ps[dIndex].InnerHtml.Substring(6).TrimAll()},{ps[dIndex + 1].InnerHtml.Substring(6).TrimAll()},{ps[dIndex + 2].InnerHtml.Substring(6).TrimAll()},{ps[dIndex + 3].InnerHtml.Substring(6).TrimAll()},{ps[dIndex + 4].InnerHtml.Substring(6).TrimAll()},",
                    CoverPicture = ps[0].Children.FirstOrDefault().GetAttribute("src").TrimAll(),
                    Description = ps[cIndex + 1].InnerHtml.TrimAll(),
                    DownloadUrlFirst = lstDownLoadURL?.FirstOrDefault(),
                    DownloadUrlSecond = lstDownLoadURL.Count() > 2 && !string.IsNullOrWhiteSpace(lstDownLoadURL[1]) ? lstDownLoadURL[1].Trim() : "",
                    DownloadUrlThird = lstDownLoadURL.Count() > 3 && !string.IsNullOrWhiteSpace(lstDownLoadURL[2]) ? lstDownLoadURL[2].Trim() : "",
                };

                movieInfo.Types = await ConvertTypeArea<MType>(types.Split('/'));
                movieInfo.Areas = await ConvertTypeArea<MArea>(area.Split('/'));

                return movieInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }


    public static class StringExtensions
    {
        public static string TrimAll(this string str)
        {
            return str.TrimAll();
        }
    }
}
