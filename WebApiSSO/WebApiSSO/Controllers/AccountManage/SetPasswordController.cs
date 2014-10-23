using WebApiSSO.Api.Filters;
using WebApiSSO.Api.Models;
using WebApiSSO.BLL;
using WebApiSSO.BLL.Business;
using System.Web.Http;

namespace WebApiSSO.Api.Controllers
{
    /// <summary>
    /// 新用户设置密码Api
    /// </summary>
    [CAuthorize]
    public class SetPasswordController : BaseDbCtrl
    {
        /// <summary>
        /// 设置密码
        /// </summary>
        /// <param name="param">设置密码参数</param>
        /// <returns></returns>
        [AllowAnonymous]
        public ResultModel Post([FromBody]SetPasswordParam param)
        {
            ResultModel rm = new ResultModel();
            if (param == null || param.Password == null || param.Password.Length < 6)
            {
                rm.State = BLL.ResultState.INV_ARGS;
                return rm;
            }
            var result = DbSession.ForWrite().GetBusiness<AccountBusiness>().SetPassword(param.UserName, param.Password, param.VerifyCode);
            rm.State = result;
            return rm;
        }

        public ResultModel Delete([FromUri]SetPasswordParam param)
        {
            ResultState state;
            if (param != null)
            {
                state = DbSession.GetBusiness<AccountBusiness>().PurgeUser(GetUserIdentity(), param.UserName, param.Password);
            } else {
                state = ResultState.INV_ARGS;
            }
            return new ResultModel(){
                State = state
            };
        }

        /// <summary>
        /// 用户注册时设置密码参数
        /// </summary>
        public class SetPasswordParam
        {
            /// <summary>
            /// 用户名
            /// </summary>
            public string UserName { set; get; }

            /// <summary>
            /// 密码
            /// </summary>
            public string Password { set; get; }

            /// <summary>
            /// 验证码
            /// </summary>
            public string VerifyCode { set; get; }
        }
    }
}