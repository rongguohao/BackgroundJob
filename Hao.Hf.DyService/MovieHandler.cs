using AngleSharp.Dom;
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

                string yearStr = null;
                if (zoomHtml.Contains("◎年　　代"))
                {
                    yearStr = zoomHtml.Split(new string[] { "◎年　　代" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
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


                if(brs.Count>10)
                {
                    if(zoomHtml.Contains("◎主　　演")&& zoomHtml.Contains("◎简"))
                    {
                        var actor = zoomHtml.Split(new string[] { "◎主　　演" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                        var actors = zoomHtml.Split(new string[] { "◎主　　演" }, 2, StringSplitOptions.None)[1].Split(new string[] { "◎简" }, 2, StringSplitOptions.None)[0].TrimAll();
                        actors = actors.Replace("<br>", ",").Replace("　", "");
                        movieInfo.MainActor = actor;
                        movieInfo.MainActors = actors;
                    }

                    if(zoomHtml.Contains("◎演　　员") && zoomHtml.Contains("◎简"))
                    {
                        var actor = zoomHtml.Split(new string[] { "◎演　　员" }, 2, StringSplitOptions.None)[1].Split(splitFeature, 2, StringSplitOptions.None)[0].TrimAll();
                        var actors = zoomHtml.Split(new string[] { "◎演　　员" }, 2, StringSplitOptions.None)[1].Split(new string[] { "◎简" }, 2, StringSplitOptions.None)[0].TrimAll();
                        actors = actors.Replace("<br>", ",").Replace("　", "");
                        movieInfo.MainActor = actor;
                        movieInfo.MainActors = actors;
                    }
                }
                else
                {
                    await Do(zoomHtml, ps, movieInfo, "◎主　　演");
                    await Do(zoomHtml,ps,movieInfo,"◎演　　员");
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


        private async Task Do(string zoomHtml, List<IElement> ps, Movie movieInfo,string tag)
        {
            if (zoomHtml.Contains(tag))
            {
                int dIndex = 0;
                while (!ps[dIndex].InnerHtml.Contains(tag))
                {
                    dIndex++;
                }

                int cIndex = 0;
                while (!ps[cIndex].InnerHtml.Contains("◎简") && !ps[cIndex].InnerHtml.Contains("◎剧情介绍") && !ps[cIndex].InnerHtml.Contains("◎内容简介"))
                {
                    cIndex++;
                }
                var actor = $"{ps[dIndex].InnerHtml.Substring(6).TrimAll()}";
                movieInfo.MainActor = actor;
                var actors = $"{ ps[dIndex].InnerHtml.Substring(6).TrimAll()}";


                int k = 1;
                while (dIndex + 1 < ps.Count - 1 && k <= 5)
                {
                    var innerhtml = ps[dIndex + 1].InnerHtml;
                    if (innerhtml.Contains("◎简") || innerhtml.Contains("◎剧情介绍") || innerhtml.Contains("◎内容简介"))
                        break;
                    actors += $",{ innerhtml.Substring(6).TrimAll()}";
                    dIndex++;
                    k++;
                }
                movieInfo.MainActors = actors;


                var description = ps[cIndex + 1].InnerHtml.TrimAll();
                movieInfo.Description = description;
                if(string.IsNullOrWhiteSpace(description))
                {
                    description = ps[cIndex + 2].InnerHtml.TrimAll();
                    movieInfo.Description = description;
                }
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
