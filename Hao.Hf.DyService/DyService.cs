using AngleSharp.Html.Dom;
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
    public partial class DyService : IDyService
    {

        protected IConfiguration _config { get; }

        protected IHttpHelper _http;

        protected static HtmlParser htmlParser = new HtmlParser();

        protected readonly string _connectionString;

        protected static IdWorker _worker = new IdWorker(1, 1);

        protected static int num = 1;

        public DyService(IHttpHelper http, IConfiguration config)
        {
            _http = http;
            _config = config;
            _connectionString = config.GetConnectionString("MySqlConnection");
        }

        public async Task PullMovieJustOnce()
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
                    await GetMovie(url, dom);

                    if (pageNum > 0)
                    {
                        for (int page = 2; page < pageNum; page++)
                        {
                            var url2 = "https://www.dy2018.com/" + i + $"/index_{page}.html";

                            //获取电影
                            await GetMovie(url2);
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


        private async Task GetMovie(string url, IHtmlDocument dom = null)
        {
            if (dom == null)
            {
                var htmlDoc = await _http.GetHtmlByUrl(url);
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

                    Movie movieInfo = await FillMovieInfoFormWeb(onlineURL);
                    if (movieInfo == null) continue;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{num++}电影名称：" + movieInfo.Name);
                    Console.WriteLine("下载地址1：" + movieInfo.DownloadUrlFirst);
                    Console.WriteLine("下载地址2：" + movieInfo.DownloadUrlSecond);
                    Console.WriteLine("下载地址3：" + movieInfo.DownloadUrlThird);
                    var success = await InsertDB(movieInfo);
                    Console.ForegroundColor = success ? ConsoleColor.Yellow : ConsoleColor.Blue;
                    Console.WriteLine(success ? "成功" : "失败");
                }
            }
        }

        /// <summary>
        /// 插入电影数据
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private async Task<bool> InsertDB(Movie info)
        {
            info.ID = _worker.NextId();

            info.CreatorID = -1;
            info.CreateTime = DateTime.Now;
            info.Creator = "系统";
            info.IsDeleted = false;
            List<MovieType> types = new List<MovieType>();
            foreach(var item in info.Types)
            {
                types.Add(new MovieType() { ID = _worker.NextId(), MID = info.ID, MType = item });
            }
            List<MovieArea> areas = new List<MovieArea>();
            foreach (var item in info.Areas)
            {
                areas.Add(new MovieArea() { ID = _worker.NextId(), MID = info.ID, MArea = item });
            }
            using (IDbConnection dbConnection = new MySqlConnection(_connectionString))
            {
                dbConnection.Open();

                IDbTransaction transaction = dbConnection.BeginTransaction();

                try
                {
                    //var sql = @"
                    //        INSERT INTO Movie (ID,Name,NameAnother,Year,ReleaseDate,Score,Director,MainActors,Description,DownloadUrlFirst,DownloadUrlSecond,DownloadUrlThird,CreateTime,CreatorID,IsDeleted,Creator,CoverPicture)  

                    //        SELECT @ID,@Name,@NameAnother,@Year,@ReleaseDate,@Score,@Director,@MainActors,@Description,@DownloadUrlFirst,@DownloadUrlSecond,@DownloadUrlThird,@CreateTime,@CreatorID,@IsDeleted,@Creator,@CoverPicture From DUAL

                    //        WHERE NOT EXISTS (SELECT 1 FROM Movie where Name = @Name)";

                    var sql = "SELECT * FROM Movie where Name = @Name";
                    var movie= dbConnection.QueryFirstOrDefault<Movie>(sql,new { info.Name});
                    if (movie != null && movie.ID.HasValue) 
                    {
                        transaction.Commit();
                        return false;
                    }

                    sql = @"
                            INSERT INTO Movie (ID,Name,NameAnother,Year,ReleaseDate,Score,Director,MainActors,Description,DownloadUrlFirst,DownloadUrlSecond,DownloadUrlThird,CreateTime,CreatorID,IsDeleted,Creator,CoverPicture)  

                            VALUES( @ID,@Name,@NameAnother,@Year,@ReleaseDate,@Score,@Director,@MainActors,@Description,@DownloadUrlFirst,@DownloadUrlSecond,@DownloadUrlThird,@CreateTime,@CreatorID,@IsDeleted,@Creator,@CoverPicture)";

                    var res = await dbConnection.ExecuteAsync(sql, info);
                    bool flag = res > 0;
                    if (flag) 
                    {
                        if(types.Count>0)
                        {
                            sql = "INSERT INTO MovieType (ID,MID,MType) Values(@ID,@MID,@MType)";

                            var a = await dbConnection.ExecuteAsync(sql, types);
                            flag = flag && a > 0;
                        }
                        if(areas.Count>0)
                        {
                            sql = "INSERT INTO MovieArea (ID,MID,MArea) Values(@ID,@MID,@MArea)";

                            var b = await dbConnection.ExecuteAsync(sql, areas);
                            flag = flag && b > 0;
                        }
                    }
                    if(flag)
                    {
                        transaction.Commit();
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                    return flag;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    transaction.Rollback();
                    return false;
                }    
            }
        }


        private async Task<List<int>> ConvertTypeArea<T>(string[] typeNames) where T : struct, IConvertible
        {
            return await Task.Factory.StartNew(() =>
           {
               List<int> list = new List<int>();

               foreach (var item in typeNames)
               {
                   var a = HDescription.GetValue(typeof(T), item.TrimAll());
                   if (a == null) continue;
                   int b = (int)a;
                   list.Add(b);
               }
               return list;
           });
        }
    }
}
