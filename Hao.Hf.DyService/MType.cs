using Hao.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hao.Hf.DyService
{
    public enum MType
    {
        [HDescription("剧情")]
        Plot = 1,

        [HDescription("喜剧")]
        Comedy,

        [HDescription("动作")]
        Action,

        [HDescription("爱情")]
        Love,

        [HDescription("科幻")]
        ScienceFiction ,

        [HDescription("动画")]
        Animation,

        [HDescription("悬疑")]
        Suspense,

        [HDescription("惊悚")]
        Horror,

        [HDescription("记录")]
        Recording,

        [HDescription("音乐歌舞")]
        MusicalDance,

        [HDescription("传记")]
        Biography,

        [HDescription("历史")]
        History,

        [HDescription("战争")]
        War,

        [HDescription("犯罪")]
        Crime,

        [HDescription("奇幻")]
        Fantasy,

        [HDescription("冒险")]
        Adventure ,

        [HDescription("灾难")]
        Disaster,

        [HDescription("武侠")]
        MartialArts,

        [HDescription("古装")]
        Costume
    }
}
