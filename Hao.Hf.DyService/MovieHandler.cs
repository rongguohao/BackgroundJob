﻿using AngleSharp.Dom;
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


                var movieInfo = new Movie();



                //电影的详细介绍 在id为Zoom的标签中
                var zoom = movieDoc.GetElementById("Zoom");

                var ps = zoom.QuerySelectorAll("p").ToList();



                var divs = zoom.QuerySelectorAll("div").ToList();
                if (divs.Count > 14)
                {
                    ps = ps.Take(2).ToList();
                    ps.AddRange(divs);
                }
                var brs = zoom.QuerySelectorAll("br").ToList();

                #region HtmlString

                var zoomHtml = zoom.InnerHtml;

                var name = movieDoc.QuerySelectorAll("div.title_all > h1").FirstOrDefault().InnerHtml.TrimAll();
                List<string> matchValue = new List<string>();
                foreach (Match m in Regex.Matches(name, "(?<=《)[^》]+(?=》)"))
                {
                    matchValue.Add(m.Value);
                }
                if (matchValue.Count > 0)
                {
                    name = matchValue.FirstOrDefault();
                    movieInfo.Name = name;
                }

                string[] splitFeature = new string[] { "<" };


                var rank = movieDoc.QuerySelector("strong.rank");

                if (rank != null)
                {
                    var score = movieDoc.QuerySelector("strong.rank").InnerHtml.TrimAll();
                    movieInfo.Score = HConvert.ToFloat(score);
                }
                else
                {
                    if (zoomHtml.Contains("◎IMDB评分"))
                    {
                        var score = zoomHtml.Split(new string[] { "◎IMDB评分" }, 2, StringSplitOptions.None)[1].Split(new string[] { "/" }, 2, StringSplitOptions.None)[0].TrimAll();
                    }
                }

                string area = null;
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
                if(!string.IsNullOrWhiteSpace(area))
                {
                    movieInfo.Areas = await ConvertTypeArea<MArea>(area.Split('/'));

                    if (movieInfo.Areas.Count < 1)
                    {
                        movieInfo.Areas.Add((int)MArea.Other);
                    }
                }


                if (zoomHtml.Contains("◎类　　别"))
                {
                    var types = zoomHtml.Split(new string[] { "◎类　　别" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                    movieInfo.Types = await ConvertTypeArea<MType>(types.Split('/'));
                    if (movieInfo.Types.Count < 1)
                    {
                        movieInfo.Types.Add((int)MType.Other);
                    }
                }

                if (zoomHtml.Contains("◎译　　名"))
                {
                    var nameother = zoomHtml.Split(new string[] { "◎译　　名" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                    movieInfo.NameAnother = nameother;
                }

                if (zoomHtml.Contains("◎又　　名"))
                {
                    var nameother = zoomHtml.Split(new string[] { "◎又　　名" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                    movieInfo.NameAnother = nameother;
                }
                if (zoomHtml.Contains("◎中文译名"))
                {
                    var nameother = zoomHtml.Split(new string[] { "◎中文译名" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                    movieInfo.NameAnother = nameother;
                }

                string yearStr = null;
                if (zoomHtml.Contains("◎年　　代"))
                {
                    yearStr = zoomHtml.Split(new string[] { "◎年　　代" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                    var year = HConvert.ToInt(yearStr.Substring(0, 4));
                    movieInfo.Year = year;
                }
                if (zoomHtml.Contains("◎出品年代"))
                {
                    yearStr = zoomHtml.Split(new string[] { "◎出品年代" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                    var year = HConvert.ToInt(yearStr.Substring(0, 4));
                    movieInfo.Year = year;
                }



                string director = null;
                if (zoomHtml.Contains("◎导　　演"))
                {
                    director = zoomHtml.Split(new string[] { "◎导　　演" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                    movieInfo.Director = director;
                }

                string releaseDate = null;
                if (zoomHtml.Contains("◎上映日期"))
                {
                    releaseDate = zoomHtml.Split(new string[] { "◎上映日期" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                }
               
                if (!string.IsNullOrWhiteSpace(releaseDate))
                {
                    releaseDate = releaseDate.Replace("月", "-").Replace("日", "-");
                    foreach (Match match in Regex.Matches(releaseDate, @"\d{4}-\d{1,2}"))
                    {
                        releaseDate = match.Groups[0].Value;
                    }
                }
                var date = HConvert.ToDateTime(releaseDate);
                if (!date.HasValue)
                {
                    date = HConvert.ToDateTime(movieInfo.Year.ToString() + "-01-01");
                }
                movieInfo.ReleaseDate = date;

                if(zoomHtml.Contains("◎主　　演")|| zoomHtml.Contains("◎演　　员"))
                {
                    var splitY = new List<string>();
                    if (zoomHtml.Contains("◎主　　演"))
                    {
                        splitY.Add("◎主　　演");
                    }
                    else if (zoomHtml.Contains("◎演　　员"))
                    {
                        splitY.Add("◎演　　员");
                    }
                    var actor = zoomHtml.Split(splitY.ToArray(), 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();

                    if (zoomHtml.Contains("◎简") || zoomHtml.Contains("◎剧情介绍") || zoomHtml.Contains("◎内容简介"))
                    {
                        var split = new List<string>();
                        if (zoomHtml.Contains("◎简"))
                        {
                            split.Add("◎简");
                        }
                        else if (zoomHtml.Contains("◎剧情"))
                        {
                            split.Add("◎剧情");
                        }
                        else if (zoomHtml.Contains("◎内容简介"))
                        {
                            split.Add("◎内容简介");
                        }
                        var actors = zoomHtml.Split(splitY.ToArray(), 2, StringSplitOptions.None)[1].Split(split.ToArray(), 2, StringSplitOptions.None)[0].TrimAll();
                        actors = actors.Replace("<br>", ",").Replace("　", "").Replace("</p>", ",").Replace("<p>", "").Replace("</div>", ",").Replace("<div>", "").Trim(',');
                        movieInfo.MainActor = actor.Replace("\r", "").Replace("\n", "");
                        movieInfo.MainActors = actors.Replace("\r", "").Replace("\n", "");

                        var html = zoomHtml.Split(split.ToArray(), 2, StringSplitOptions.None)[1];
                        if (html.Contains("◎") || html.Contains("一句话评论")||html.Contains("幕后制作") || html.Contains("幕后故事")||html.Contains("幕后：")) 
                        {
                            var spt = new List<string>();
                            if(html.Contains("◎"))
                            {
                                spt.Add("◎");
                            }
                            else if(html.Contains("一句话评论"))
                            {
                                spt.Add("一句话评论");
                            }
                            else if (html.Contains("幕后制作"))
                            {
                                spt.Add("幕后制作");
                            }
                            else if (html.Contains("幕后故事"))
                            {
                                spt.Add("幕后故事");
                            }
                            else if (html.Contains("幕后："))
                            {
                                spt.Add("幕后：");
                            }
                            else
                            {
                                spt.Add("<img");
                            }
                            var htmldes = html.Split(spt.ToArray(),2,StringSplitOptions.None)[0].TrimAll().Replace("\r", "").Replace("\n", "").Trim(',');
                            htmldes = htmldes.Replace("<br>", ",").Replace("　", "").Replace("</p>", ",").Replace("<p>", "").Replace("</div>", ",").Replace("<div>", "").Trim(',');
      
                            if(htmldes.Substring(0,4)== "介 , ")
                            {
                                htmldes = htmldes.Substring(4);
                            }
                            else if (htmldes.Substring(0, 2) == "介,")
                            {
                                htmldes = htmldes.Substring(2);
                            }
                            movieInfo.Description = htmldes.Trim(',').Trim(' ');
                        }
                    }
                }


                var a = ps[0].Children.FirstOrDefault();
                if (a != null) 
                {
                    var pUrl = a.GetAttribute("src");
                    if(pUrl != null)
                    {
                        movieInfo.CoverPicture = pUrl;
                    }

                }

                string[] splitUrl = new string[] { "\"" };
                string first = null, second = null, third = null;
                if (zoomHtml.Contains("\"magnet:?"))
                {
                    first = "magnet:?" + zoomHtml.Split(new string[] { "\"magnet:?" }, 2, StringSplitOptions.None)[1].Split(splitUrl, 2, StringSplitOptions.None)[0].TrimAll();
                    movieInfo.DownloadUrlFirst = first;
                }
                if (zoomHtml.Contains("\"thunder://"))
                {
                    second = "thunder://" + zoomHtml.Split(new string[] { "\thunder://" }, 2, StringSplitOptions.None)[1].Split(splitUrl, 2, StringSplitOptions.None)[0].TrimAll();
                    movieInfo.DownloadUrlSecond = second;
                }
                if (zoomHtml.Contains("ftp://"))
                {
                    third = "ftp://" + zoomHtml.Split(new string[] { "ftp://" }, 2, StringSplitOptions.None)[1].Split(new string[] { "<" }, 2, StringSplitOptions.None)[0].TrimAll();
                    movieInfo.DownloadUrlThird = third;
                }

                #endregion

                return movieInfo;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(onlineURL);
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
