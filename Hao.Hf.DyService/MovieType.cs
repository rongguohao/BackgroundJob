using Hao.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hao.Hf.DyService
{
    public enum MovieType
    {
        [HDescription("剧情")]
        Plot = 1,

        [HDescription("喜剧")]
        Comedy = 2,

        [HDescription("动作")]
        Action = 3,

        [HDescription("爱情")]
        Love = 4,

        [HDescription("科幻")]
        ScienceFiction = 5,

        [HDescription("动画")]
        Animation = 6,

        [HDescription("悬疑")]
        Suspense = 7,

        [HDescription("惊悚")]
        Horror = 8,

        [HDescription("记录")]
        Recording = 9,

        [HDescription("音乐歌舞")]
        MusicalDance = 10,

        [HDescription("传记")]
        Biography = 11,

        [HDescription("历史")]
        History = 12,

        [HDescription("战争")]
        War = 13,

        [HDescription("犯罪")]
        Crime = 14,

        [HDescription("奇幻")]
        Fantasy = 15,

        [HDescription("冒险")]
        Adventure = 16,

        [HDescription("灾难")]
        Disaster = 17,

        [HDescription("武侠")]
        MartialArts = 18,

        [HDescription("古装")]
        Costume = 19
    }
}
