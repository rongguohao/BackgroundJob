﻿using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Dapper;
using Hao.Utility;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Snowflake.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hao.Hf.DyService
{
    public class DyService : IDyService
    {

        protected IConfiguration _config { get; }

        protected IHttpHelper _http;

        protected static HtmlParser htmlParser = new HtmlParser();

        protected static string _connectionString;

        protected static IdWorker _worker = new IdWorker(1, 1);

        protected static int num = 1;

        protected static int count = 0; 

        public DyService(IHttpHelper http,IConfiguration config)
        {
            _http = http;
            _config = config;
            _connectionString = config.GetConnectionString("MySqlConnection");
        }


        public async Task PullMovie()
        {
            try
            {
                for (int i = 0; i < 21; i++)
                {
                    //拼接成完整链接
                    var url = "https://www.dy2018.com/" + i + "/";

                    var htmlDoc = await _http.GetHtmlByUrl(url);
                    if (string.IsNullOrWhiteSpace(htmlDoc)) continue;
                    var dom = htmlParser.ParseDocument(htmlDoc);

                    var pContent = dom.QuerySelectorAll("div.x > p");
                    int pageNum = 0;
                    if (pContent != null && pContent.Length > 0)
                    {
                        var content = pContent.FirstOrDefault().InnerHtml.Split(new String[1] { "&nbsp;" }, 2, StringSplitOptions.None).FirstOrDefault().Split('/')[1];
                        pageNum = Convert.ToInt32(content);
                    }

                    //获取电影
                    var flag = await GetMovie(_http, url, dom);
                    if (!flag) continue;

                    if (pageNum > 0)
                    {
                        for (int page = 2; page < pageNum; page++)
                        {
                            var url2 = "https://www.dy2018.com/" + i + $"/index_{page}.html";

                            //获取电影
                            bool flag2 = await GetMovie(_http, url2);
                            if (!flag2) continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("异常：" + ex.ToString());
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("结束！！！！");
                Console.ReadKey();
            }
        }


        private static async Task<bool> GetMovie(IHttpHelper http, string url, IHtmlDocument dom = null)
        {
            if (dom == null)
            {
                var htmlDoc = await http.GetHtmlByUrl(url);
                if (string.IsNullOrWhiteSpace(htmlDoc)) return true;
                dom = htmlParser.ParseDocument(htmlDoc);
            }
            var tables = dom.QuerySelectorAll("table.tbspan");

            if (tables != null && tables.Count() > 0)
            {
                count = 0;//初始化0
                foreach (var tb in tables)
                {
                    var href = tb.QuerySelectorAll("a").Where(a => a.GetAttribute("href").Contains(".html")).FirstOrDefault();
                    //拼接成完整链接
                    var onlineURL = "http://www.dy2018.com" + href.GetAttribute("href");


                    Movie movieInfo = await FillMovieInfoFormWeb(http, onlineURL);
                    if (movieInfo == null) continue;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{num++}电影名称：" + movieInfo.Name);
                    Console.WriteLine("下载地址：" + movieInfo.DownloadUrlFirst);
                    var success = await InsertDB(movieInfo);
                    Console.ForegroundColor = success ? ConsoleColor.Yellow : ConsoleColor.Blue;
                    Console.WriteLine(success ? "成功" : "失败");
                    if (!success) count++;
                    if (count > 10) return false;
                }
            }
            return true;
        }


        private static async Task<Movie> FillMovieInfoFormWeb(IHttpHelper http, string onlineURL)
        {
            try
            {
                var movieHTML = await http.GetHtmlByUrl(onlineURL);
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
                if(string.IsNullOrWhiteSpace(releaseDate))
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

                var movieInfo = new Movie()
                {
                    Name = movieDoc.QuerySelectorAll("div.title_all > h1").FirstOrDefault().InnerHtml,
                    NameAnother = ps[1].InnerHtml.Substring(6).Replace("&nbsp;",""),
                    Year = HConvert.ToInt(ps[3].InnerHtml.Substring(6)),
                    Area = ps[4].InnerHtml.Substring(6),
                    Types = ConvertTypes(ps[5].InnerHtml.Substring(6).Split('/')),
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
            catch (Exception ex)
            {

                return null;
            }

        }

        /// <summary>
        /// 插入电影数据
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private static async Task<bool> InsertDB(Movie info)
        {
            info.ID = _worker.NextId();
            List<string> matchValue = new List<string>();
            foreach (Match m in Regex.Matches(info.Name, "(?<=《)[^》]+(?=》)"))
            {
                matchValue.Add(m.Value);
            }
            if (matchValue.Count > 0)
            {
                info.Name = matchValue.FirstOrDefault();
            }
            info.CreatorID = -1;
            info.CreateTime = DateTime.Now;
            info.Creator = "系统";
            info.IsDeleted = false;
            using (IDbConnection dbConnection = new MySqlConnection(_connectionString))
            {
                dbConnection.Open();

                var sql = @"
                            INSERT INTO Movie (ID,Name,NameAnother,Year,Area,Types,ReleaseDate,Score,Director,MainActors,Description,DownloadUrlFirst,DownloadUrlSecond,DownloadUrlThird,CreateTime,CreatorID,IsDeleted,Creator,CoverPicture)  
                                       
                            SELECT @ID,@Name,@NameAnother,@Year,@Area,@Types,@ReleaseDate,@Score,@Director,@MainActors,@Description,@DownloadUrlFirst,@DownloadUrlSecond,@DownloadUrlThird,@CreateTime,@CreatorID,@IsDeleted,@Creator,@CoverPicture From DUAL

                            WHERE NOT EXISTS (SELECT 1 FROM Movie where Name = @Name)";

                var res = await dbConnection.ExecuteAsync(sql, info);

                return res > 0;
            }
        }


        private static string ConvertTypes(string[] typeNames)
        {
            string types = "";
            int index = 0;
            foreach (var item in typeNames)
            {
                var a = HDescription.GetValue(typeof(MovieType), item);
                if (a == null) continue;
                int b = (int)a;
                if (index == 0)
                {
                    if (b > 0)
                    {
                        types += $",{b},";
                    }
                }
                else
                {
                    if (b > 0)
                    {
                        types += $"{b},";
                    }
                }
                index++;
            }
            return types;
        }
    }
}
