using Hao.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hao.Hf.DyService
{
    public enum MArea
    {
        [HDescription("中国大陆")]
        China=1,
        [HDescription("香港")]
        HongKong,
        [HDescription("台湾")]
        TaiWan,
        [HDescription("美国")]
        USA,
        [HDescription("日本")]
        RiBen,
        [HDescription("韩国")]
        HangGuo,
        [HDescription("泰国")]
        TaiGuo,
        [HDescription("印度")]
        YinDu,
        [HDescription("英国")]
        YingGuo,
        [HDescription("法国")]
        FaGuo,
        [HDescription("德国")]
        DeGuo,
        [HDescription("俄罗斯")]
        EluoSi,
        [HDescription("加拿大")]
        JiaNaDa,
        [HDescription("澳大利亚")]
        AoDaLiYa,
        [HDescription("西班牙")]
        XiBanYa,
        [HDescription("比利时")]
        BiLiShi,
        [HDescription("其他")]
        Other,
    }
}
