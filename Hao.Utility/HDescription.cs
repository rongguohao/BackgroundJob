﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Hao.Utility
{
    /// <summary>
    /// 获取枚举描述
    /// </summary>
    public class HDescription
    {

        private static ConcurrentDictionary<Type, HDescriptionAttribute[]> enumCache = new ConcurrentDictionary<Type, HDescriptionAttribute[]>();
        /// <summary>
        /// 获取所有字段
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <returns></returns>

        public static HDescriptionAttribute[] Get(Type enumType)
        {
            return HDescription.enumCache.GetOrAdd(enumType, (Type type) => type.GetFields(BindingFlags.Static | BindingFlags.Public).Select(new Func<FieldInfo, HDescriptionAttribute>(HDescription.Get)).ToArray<HDescriptionAttribute>());
        }


        private static HDescriptionAttribute Get(FieldInfo fieldInfo)
        {
            HDescriptionAttribute customAttribute = fieldInfo.GetCustomAttribute<HDescriptionAttribute>();
            if (customAttribute == null)
            {
                return null;
            }

            return customAttribute.With(b =>
            {
                b.Field = fieldInfo;
            });
        }



        /// <summary>
        /// 获取字段
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns></returns>
        public static HDescriptionAttribute Get(Type enumType, string description)
        {
            return HDescription.Get(enumType).SingleOrDefault((HDescriptionAttribute d) => d.Description == description);
        }

        /// <summary>
        /// 获取字段
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static HDescriptionAttribute Get<T>(Type enumType, T value) where T : struct, IConvertible
        {
            return HDescription.Get(enumType).SingleOrDefault(delegate (HDescriptionAttribute item)
            {
                T t = (T)((object)item.Value);
                return t.Equals(value);
            });
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <param name="description">字段名称</param>
        /// <returns></returns>
        public static IConvertible GetValue(Type enumType, string description)
        {
            HDescriptionAttribute HDescriptionAttribute = HDescription.Get(enumType, description);
            if (HDescriptionAttribute == null)
            {
                return null;
            }
            return HDescriptionAttribute.Value;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="enumType">枚举类型</param>
        /// <param name="description">字段名称</param>
        /// <returns></returns>
        public static T GetValue<T>(Type enumType, string description) where T : struct, IConvertible
        {
            HDescriptionAttribute HDescriptionAttribute = HDescription.Get(enumType, description);
            if (HDescriptionAttribute == null)
            {
                return default(T);
            }
            return HDescriptionAttribute.GetValue<T>();
        }

        /// <summary>
        /// 获取描述
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <param name="description">字段名称</param>
        /// <returns></returns>
        public static string GetDescription(Type enumType, string description)
        {
            HDescriptionAttribute HDescriptionAttribute = HDescription.Get(enumType, description);
            if (HDescriptionAttribute == null)
            {
                return null;
            }
            return HDescriptionAttribute.Description;
        }

        /// <summary>
        /// 获取描述
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static string GetDescription(Type enumType, int value)
        {
            HDescriptionAttribute HDescriptionAttribute = HDescription.Get<int>(enumType, value);
            if (HDescriptionAttribute == null)
            {
                return null;
            }
            return HDescriptionAttribute.Description;
        }

        /// <summary>
        /// 获取描述
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static string GetDescription<T>(Type enumType, T value) where T : struct, IConvertible
        {
            HDescriptionAttribute HDescriptionAttribute = HDescription.Get<T>(enumType, value);
            if (HDescriptionAttribute == null)
            {
                return null;
            }
            return HDescriptionAttribute.Description;
        }

        /// <summary>
        /// 获取描述
        /// </summary>
        /// <param name="enum">枚举对象</param>
        /// <returns></returns>
        public static string GetDescription(Enum @enum)
        {
            HDescriptionAttribute HDescriptionAttribute = HDescription.Get(@enum.GetType(), @enum.ToString());
            if (HDescriptionAttribute == null)
            {
                return null;
            }
            return HDescriptionAttribute.Description;
        }
    }
}
