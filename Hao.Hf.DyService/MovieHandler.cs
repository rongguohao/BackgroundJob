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

                //var lstDownLoadURL = movieDoc.QuerySelectorAll("td > a").Where(a => !a.GetAttribute("href").Contains(".html")).Select(a => a.InnerHtml).ToList();

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

                string[] splitFeature = new string[] { "<" };

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
                }
                var date = HConvert.ToDateTime(releaseDate);
                if (!date.HasValue)
                {
                    date = HConvert.ToDateTime(year.ToString() + "-01-01");
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
                string[] splitUrl = new string[] { "\"" };
                string first = null, second = null, third = null;
                if(zoomHtml.Contains("\"thunder://"))
                {
                    first = "thunder://" + zoomHtml.Split(new string[] { "\thunder://" }, 2, StringSplitOptions.None)[1].Split(splitUrl, 2, StringSplitOptions.None)[0].TrimAll();
                }
                if (zoomHtml.Contains("\"magnet:?"))
                {
                    second = "magnet:?" + zoomHtml.Split(new string[] { "\"magnet:?" }, 2, StringSplitOptions.None)[1].Split(splitUrl, 2, StringSplitOptions.None)[0].TrimAll();
                }
                if (zoomHtml.Contains("ftp://"))
                {
                    third = "ftp://" + zoomHtml.Split(new string[] { "ftp://" }, 2, StringSplitOptions.None)[1].Split(new string[] { "<" }, 2, StringSplitOptions.None)[0].TrimAll();
                }

                var actor = $"{ps[dIndex].InnerHtml.Substring(6).TrimAll()}";
                var actors = $"{ ps[dIndex].InnerHtml.Substring(6).TrimAll()}";
                var picture = ps[0].Children.FirstOrDefault().GetAttribute("src").TrimAll();
                var description = ps[cIndex + 1].InnerHtml.TrimAll();
                while (dIndex < ps.Count) 
                {
                    actors += $",{ ps[dIndex+1].InnerHtml.Substring(6).TrimAll()}";
                    dIndex++;
                }
                #endregion

                var movieInfo = new Movie()
                {
                    Name = name,
                    NameAnother = nameother,
                    Year = year,
                    ReleaseDate = date,
                    Score = HConvert.ToFloat(score),
                    Director = director,
                    MainActor = actor,
                    MainActors = actor,
                    CoverPicture = picture,
                    Description = description,
                    DownloadUrlFirst = first,
                    DownloadUrlSecond = second,
                    DownloadUrlThird = third,
                };

                movieInfo.Types = await ConvertTypeArea<MType>(types.Split('/'));
                movieInfo.Areas = await ConvertTypeArea<MArea>(area.Split('/'));
                if (movieInfo.Types.Count < 1)
                {
                    movieInfo.Types.Add((int)MType.Other);
                }
                if (movieInfo.Areas.Count < 1) 
                {
                    movieInfo.Areas.Add((int)MArea.Other);
                }
                return movieInfo;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }


    public static class StringExtensions
    {
        public static string TrimAll(this string str)
        {
            return str.Replace("&nbsp;","").Trim();
        }
    }
}
