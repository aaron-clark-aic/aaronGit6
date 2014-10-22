using System;
using System.Linq;

namespace WebApiSSO.Api.Models.Utils
{
    public class ParamHelper
    {
        /// <summary>
        /// 检查类参数是否有空值
        /// </summary>
        /// <param name="ob">对象</param>
        /// <param name="type">类型</param>
        /// <param name="ignore">可忽略参数</param>
        /// <returns></returns>
        static public bool CheckAllAttNoEmpty(object ob, Type type, params string[] ignore)
        {
            foreach (var p in type.GetProperties())
            {
                if (ignore.Contains(p.Name)) continue;
                if (p.GetValue(ob, null) == null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}