using WebApiSSO.Api.Filters;
using WebApiSSO.Api.Models;
using WebApiSSO.BLL;
using WebApiSSO.BLL.Business;
using WebApiSSO.DAL;
using System;
using System.Web.Http;

namespace WebApiSSO.Api.Controllers
{
    [CAuthorize]
    public class UserInfoController : BaseDbCtrl
    {
        /// <summary>
        /// 获取用户信息(自己或他人)
        /// </summary>
        /// <returns></returns>
        public ResultModel<BaseUser> Get(string username = null)
        {
            ResultModel<BaseUser> rm = new ResultModel<BaseUser>();
            BaseUser user;
            rm.State = DbSession.GetBusiness<AccountBusiness>().GetUserInfo(this.GetUserIdentity(), username, out user);
            rm.Data = user;
            return rm;
        }

        #region 单个信息更新（弃用）
        /// <summary>
        /// 更新我的基本信息
        /// </summary>
        /// <param name="t_v">信息参数</param>
        /// <returns></returns>
        /* public ResultModel Post([FromBody]AttrValueParam t_v)
         {
             ResultModel<User> rm = new ResultModel<User>();
             if (t_v == null)
             {
                 rm.State = BLL.ResultState.INV_ARGS;
                 return rm;
             }
             rm.State = DbSession.GetBusiness<AccountBusiness>().UpdateUserInfo_OneItem(GetUserIdentity(), t_v.Attr, t_v.Value);
             return rm;
         }
         */
        #endregion

        /// <summary>
        /// 更新我的基本信息
        /// </summary>
        /// <param name="baseUser">用户信息</param>
        /// <returns></returns>
        public ResultModel Post([FromBody]UserUpateInfo updatedUserInfo)
        {

            if (updatedUserInfo == null)
            {
                return new ResultModel()
                {
                    State = ResultState.INV_ARGS
                };
            }
            ResultModel rm = new ResultModel();
            var id = this.GetUserIdentity().Id;
            updatedUserInfo.ID = id;
            rm.State = DbSession.GetBusiness<AccountBusiness>().UpdateUserInfo(GetUserIdentity(), updatedUserInfo);
            return rm;
        }


        /// <summary>
        /// 属性名-值 参数类
        /// </summary>
        public class AttrValueParam
        {
            public string Attr { get; set; }
            public object Value { get; set; }
        }
    }
}