﻿using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Dapper;
using Hao.Utility;
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
        protected IHttpHelper _http;

        protected static HtmlParser htmlParser = new HtmlParser();

        protected const string _connectionString = "Data Source=119.27.173.241;Database=haohaoPlay;User ID=root;Password=Mimashi@7758258;CharSet=utf8;port=3306;sslmode=none";

        protected static IdWorker _worker = new IdWorker(1, 1);

        protected static int num = 1;

        public DyService(IHttpHelper http)
        {
            _http = http;
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
                    await GetMovie(_http, url, dom);

                    if (pageNum > 0)
                    {
                        for (int page = 2; page < pageNum; page++)
                        {
                            var url2 = "https://www.dy2018.com/" + i + $"/index_{page}.html";

                            //获取电影
                            await GetMovie(_http, url2);

                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }



        private static async Task GetMovie(IHttpHelper http, string url, IHtmlDocument dom = null)
        {
            if (dom == null)
            {
                var htmlDoc = await http.GetHtmlByUrl(url);
                if (string.IsNullOrWhiteSpace(htmlDoc)) return;
                dom = htmlParser.ParseDocument(htmlDoc);
            }
            var tables = dom.QuerySelectorAll("table.tbspan");

            if (tables != null && tables.Count() > 0)
            {
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
                }
            }
        }


        private static async Task<Movie> FillMovieInfoFormWeb(IHttpHelper http, string onlineURL)
        {
            var movieHTML = await http.GetHtmlByUrl(onlineURL);
            if (string.IsNullOrWhiteSpace(movieHTML)) return null;
            var movieDoc = htmlParser.ParseDocument(movieHTML);
            //电影的详细介绍 在id为Zoom的标签中
            var zoom = movieDoc.GetElementById("Zoom");

            var ps = zoom.QuerySelectorAll("p").ToList();

            var divs = zoom.QuerySelectorAll("div").ToList();
            if (divs.Count > 5)
            {
                ps = ps.Take(2).ToList();
                ps.AddRange(divs);
            }

            var lstDownLoadURL = movieDoc.QuerySelectorAll("td > a").Select(a => a.InnerHtml);


            var movieInfo = new Movie()
            {
                Name = movieDoc.QuerySelectorAll("div.title_all > h1").FirstOrDefault().InnerHtml,
                NameAnother = ps[1].InnerHtml.Substring(6),
                Year = HConvert.ToInt(ps[3].InnerHtml.Substring(6)),
                Area = ps[4].InnerHtml.Substring(6),
                Types = ConvertTypes(ps[5].InnerHtml.Substring(6).Split('/')),
                ReleaseDate = HConvert.ToDateTime(ps[8].InnerHtml.Substring(6, 10)),
                Score = HConvert.ToDouble(ps[9].InnerHtml.Substring(6, 3)),
                DownloadUrlFirst = lstDownLoadURL?.FirstOrDefault(),
            };
            return movieInfo;
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
            using (IDbConnection dbConnection = new MySqlConnection(_connectionString))
            {
                dbConnection.Open();

                var sql = @" INSERT INTO Movie (ID,Name,NameAnother,Year,Area,Types,ReleaseDate,Score)  
                                        VALUES (@ID,@Name,@NameAnother,@Year,@Area,@Types,@ReleaseDate,@Score)";

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