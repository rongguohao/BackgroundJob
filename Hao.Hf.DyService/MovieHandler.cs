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

                string releaseDate = "";
                string date = ps[8].InnerHtml;
                string date2 = "";
                if (ps[8].InnerHtml.Contains("/"))
                {
                    date = ps[8].InnerHtml.Split('/')[0];
                    date2 = ps[8].InnerHtml.Split('/')[1];
                }
                foreach (Match match in Regex.Matches(date, @"\d{4}-\d{1,2}-\d{1,2}"))
                {
                    releaseDate = match.Groups[0].Value;
                }
                if (string.IsNullOrWhiteSpace(releaseDate))
                {
                    foreach (Match match in Regex.Matches(date2, @"\d{4}-\d{1,2}-\d{1,2}"))
                    {
                        releaseDate = match.Groups[0].Value;
                    }
                }
                if (string.IsNullOrWhiteSpace(releaseDate))
                {
                    date = ps[7].InnerHtml;
                    date2 = "";
                    if (ps[7].InnerHtml.Contains("/"))
                    {
                        date = ps[7].InnerHtml.Split('/')[0];
                        date2 = ps[7].InnerHtml.Split('/')[1];
                    }
                    foreach (Match match in Regex.Matches(date, @"\d{4}-\d{1,2}-\d{1,2}"))
                    {
                        releaseDate = match.Groups[0].Value;
                    }
                    if (string.IsNullOrWhiteSpace(releaseDate))
                    {
                        foreach (Match match in Regex.Matches(date2, @"\d{4}-\d{1,2}-\d{1,2}"))
                        {
                            releaseDate = match.Groups[0].Value;
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(releaseDate))
                {
                    date = ps[8].InnerHtml;
                    date2 = "";
                    if (ps[8].InnerHtml.Contains("/"))
                    {
                        date = ps[8].InnerHtml.Split('/')[0];
                        date2 = ps[8].InnerHtml.Split('/')[1];
                    }
                    foreach (Match match in Regex.Matches(date, @"\d{4}-\d{1,2}"))
                    {
                        releaseDate = match.Groups[0].Value;
                    }
                    if (string.IsNullOrWhiteSpace(releaseDate))
                    {
                        foreach (Match match in Regex.Matches(date2, @"\d{4}-\d{1,2}"))
                        {
                            releaseDate = match.Groups[0].Value;
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(releaseDate))
                {
                    releaseDate = ps[3].InnerHtml.Substring(6);
                }

                string directorTag = "导　　演";
                string director = "";
                int dIndex = 14;
                if (ps[14].InnerHtml.Contains(directorTag))
                {
                    dIndex = 14;
                    director = ps[14].InnerHtml.Substring(6);
                }
                else if (ps[15].InnerHtml.Contains(directorTag))
                {
                    dIndex = 15;
                    director = ps[15].InnerHtml.Substring(6);
                }
                else if (ps[16].InnerHtml.Contains(directorTag))
                {
                    dIndex = 16;
                    director = ps[16].InnerHtml.Substring(6);
                }

                int cIndex = dIndex + 6;
                while (!ps[cIndex].InnerHtml.Contains("简　　介"))
                {
                    cIndex++;
                }

                var zoomHtml = movieDoc.GetElementById("Zoom").InnerHtml;

                var area = "";
                if (zoomHtml.Contains("◎产　　地"))
                {
                    area = zoomHtml.Split(new string[] { "◎产　　地" }, 2, StringSplitOptions.None)[1].Split(new string[] { "</p>" }, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();
                }
                else if (zoomHtml.Contains("◎国　　家"))
                {
                    area = zoomHtml.Split(new string[] { "◎国　　家" }, 2, StringSplitOptions.None)[1].Split(new string[] { "</p>" }, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();
                }

                var nameother = zoomHtml.Split(new string[] { "◎译　　名" }, 2, StringSplitOptions.None)[1].Split(new string[] { "</p>" }, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();

                var types = zoomHtml.Split(new string[] { "◎类　　别" }, 2, StringSplitOptions.None)[1].Split(new string[] { "</p>" }, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim();
                var movieInfo = new Movie()
                {
                    Name = movieDoc.QuerySelectorAll("div.title_all > h1").FirstOrDefault().InnerHtml,
                    //NameAnother = ps[1].InnerHtml.Substring(6).Replace("&nbsp;", ""),
                    NameAnother = nameother,
                    //Year = HConvert.ToInt(ps[3].InnerHtml.Substring(6)),
                    Year = HConvert.ToInt(zoomHtml.Split(new string[] { "◎年　　代" }, 2, StringSplitOptions.None)[1].Split(new string[] { "</p>" }, 2, StringSplitOptions.None)[0].Replace("&nbsp;", "").Trim()),
                    //Area = ps[4].InnerHtml.Substring(6),
                    Area = area,
                    //Types = await ConvertTypes(ps[5].InnerHtml.Substring(6).Split('/')),
                    Types = await ConvertTypes(types.Split('/')),
                    ReleaseDate = HConvert.ToDateTime(releaseDate),
                    Score = HConvert.ToFloat(score),
                    Director = director,
                    MainActors = $",{ps[dIndex + 1].InnerHtml.Substring(6)},{ps[dIndex + 2].InnerHtml.Substring(6)},{ps[dIndex + 3].InnerHtml.Substring(6)},{ps[dIndex + 4].InnerHtml.Substring(6)},{ps[dIndex + 5].InnerHtml.Substring(6)},",
                    CoverPicture = ps[0].Children.FirstOrDefault().GetAttribute("src"),
                    Description = ps[cIndex + 1].InnerHtml,
                    DownloadUrlFirst = lstDownLoadURL?.FirstOrDefault(),
                    DownloadUrlSecond = lstDownLoadURL.Count() > 2 && !string.IsNullOrWhiteSpace(lstDownLoadURL[1]) ? lstDownLoadURL[1] : "",
                    DownloadUrlThird = lstDownLoadURL.Count() > 3 && !string.IsNullOrWhiteSpace(lstDownLoadURL[2]) ? lstDownLoadURL[2] : "",
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
