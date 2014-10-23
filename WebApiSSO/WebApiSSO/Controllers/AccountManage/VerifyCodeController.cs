using WebApiSSO.Api.Models;
using WebApiSSO.BLL.Business;
using System.Web.Http;

namespace WebApiSSO.Api.Controllers
{
    /// <summary>
    /// 验证码Api
    /// </summary>
    [AllowAnonymous]
    public class VerifyCodeController : BaseDbCtrl
    {
        /// <summary>
        /// 获取验证码
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="type">验证码类型（注册，修改密码等）</param>
        /// <returns></returns>
        public ResultModel Get(string username, VerifyCodeType? type)
        {
            //验证参数
            if (string.IsNullOrEmpty(username) || type == null)
            {
                return new ResultModel()
                {
                    State = BLL.ResultState.INV_ARGS
                };
            }
            var state = DbSession.GetBusiness<AccountBusiness>().RuqestMsgVerifyCode(username, (VerifyCodeType)type);
            ResultModel rm = new ResultModel();
            rm.State = state;
            //rm.Desc = val;
            return rm;
        }

        /// <summary>
        /// 验证验证码
        /// </summary>
        /// <param name="param">验证验证码参数</param>
        /// <returns></returns>
        public ResultModel Post([FromBody]CheckVerifyCodeParam param)
        {
            ResultModel rm = new ResultModel();
            if (param == null)
            {
                rm.State = BLL.ResultState.INV_ARGS;
                return rm;
            }
            var state = DbSession.GetBusiness<AccountBusiness>().CheckMsgVerifyCode(param.Username, param.VerifyCode, (VerifyCodeType)param.Type);        
            rm.State = state;
            return rm;
        }

        /// <summary>
        /// 验证验证码参数类
        /// </summary>
        public class CheckVerifyCodeParam
        {
            /// <summary>
            /// 用户名
            /// </summary>
            public string Username { set; get; }

            /// <summary>
            /// 验证码
            /// </summary>
            public string VerifyCode { set; get; }

            /// <summary>
            /// 验证码类型
            /// </summary>
            public VerifyCodeType? Type { set; get; }
        }
    }
}