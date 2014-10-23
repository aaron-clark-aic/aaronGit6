using WebApiSSO.Api.Filters;
using WebApiSSO.Api.Models;
using WebApiSSO.Api.Models.Utils;
using WebApiSSO.BLL.Business;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace WebApiSSO.Api.Controllers
{
    [CAuthorize]
    public class UserSearchController : BaseDbCtrl
    {
        // GET api/usersearch
        /// <summary>
        /// 查询用户信息（模糊）
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        public ResultModel<IEnumerable<BaseUser>> Get([FromUri]Param param)
        {
            List<BaseUser> user = null;
            if (param.KeyWord == null)
            {
                param.KeyWord = string.Empty;
            }
            if (!ParamHelper.CheckAllAttNoEmpty(param, param.GetType(), "KeyWord"))
            {
                return new ResultModel<IEnumerable<BaseUser>>()
                {
                    State = BLL.ResultState.INV_ARGS,
                    Data = new List<BaseUser>()
                };
            }
            var state = DbSession.GetBusiness<AccountBusiness>().SearchUser(param.KeyWord, (int)param.Index, (int)param.Count, out user);
            return new ResultModel<IEnumerable<BaseUser>>()
            {
                State = state,
                Data = user ?? new List<BaseUser>()
            };
        }

        public class Param
        {
            /// <summary>
            /// 关键字
            /// </summary>
            private string keyWord = String.Empty;

            public string KeyWord
            {
                get { return keyWord; }
                set { keyWord = value; }
            }
            /// <summary>
            /// 索引
            /// </summary>
            public int? Index { get; set; }
            /// <summary>
            /// 返回数量
            /// </summary>
            public int? Count { get; set; }
        }
    }
}
