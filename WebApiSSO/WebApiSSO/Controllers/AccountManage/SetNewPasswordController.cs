using WebApiSSO.Api.Filters;
using WebApiSSO.Api.Models;
using WebApiSSO.BLL;
using WebApiSSO.BLL.Business;
using System;
using System.Web.Http;

namespace WebApiSSO.Api.Controllers.AccountManage
{
    [CAuthorize]
    public class SetNewPasswordController : BaseDbCtrl
    {
        public ResultModel Post(SetNewPasswordParam param)
        {
            if (param == null || param.NPassword == null || param.OPassword == null || param.NPassword.Length < 6)
            {
                return new ResultModel()
                {
                    State = BLL.ResultState.INV_ARGS
                };
            }
            var res = DbSession.GetBusiness<AccountBusiness>().SetNewPassword(this.User.Identity.Name, param.OPassword, param.NPassword);
            return new ResultModel()
            {
                State = res
            };
        }

        /// <summary>
        /// 设置新密码参数类
        /// </summary>
        public class SetNewPasswordParam
        {
            /// <summary>
            /// 旧密码
            /// </summary>
            public string OPassword { set; get; }

            /// <summary>
            /// 新密码
            /// </summary>
            public string NPassword { set; get; }
        }
    }
}
