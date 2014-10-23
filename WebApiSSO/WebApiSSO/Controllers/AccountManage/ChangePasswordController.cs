using WebApiSSO.Api.Models;
using WebApiSSO.BLL.Business;
using System.Web.Http;

namespace WebApiSSO.Api.Controllers
{
    /// <summary>
    /// 用户修改密码Api(同样适用于找回密码)
    /// </summary>
    [AllowAnonymous]
    public class ChangePasswordController : BaseDbCtrl
    {
        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="param">修改密码参数</param>
        /// <returns></returns>
        public ResultModel Post([FromBody]ChangePasswordParam param)
        {
            ResultModel rm = new ResultModel();
            if (param == null)
            {
                rm.State = BLL.ResultState.INV_ARGS;
                return rm;
            }
            var result = DbSession.ForWrite().GetBusiness<AccountBusiness>().ChangePassword(param.UserName, param.NewPassword, param.VerifyCode);
            rm.State = result;
            return rm;
        }

        /// <summary>
        /// 修改密码参数
        /// </summary>
        public class ChangePasswordParam
        {
            /// <summary>
            /// 用户名
            /// </summary>
            public string UserName { set; get; }

            /// <summary>
            /// 新密码
            /// </summary>
            public string NewPassword { set; get; }
            
            /// <summary>
            /// 验证码
            /// </summary>
            public string VerifyCode { set; get; }
        }
    }
}