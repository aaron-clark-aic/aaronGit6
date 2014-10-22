using WebApiSSO.Api.Models;
using WebApiSSO.Api.Models.Params;
using WebApiSSO.Api.Models.Utils;
using WebApiSSO.BLL;
using WebApiSSO.BLL.Token;
using System.Web.Http;

namespace WebApiSSO.Api.Controllers
{
    /// <summary>
    /// 获取和管理用户鉴权信息
    /// </summary>
    public class TokenController : BaseDbCtrl
    {
        private const string COOKIE_NAME = "TOKEN";
        private readonly ITokenProvider tokenProv;

        public TokenController()
        {
            tokenProv = this.DbSession.GetITokenProvider();
        }

        //868218666650
        //仅PUT和POST建议使用正文穿参, 否则经过代理服务器时可能造成正文丢失

        // POST api/token/
        //{"usr":"18912345678","pwd":"12345678", "client": 1}
        /// <summary>
        /// 获取登录Token
        /// </summary>
        /// <param name="loginInfo">登录信息</param>
        /// <returns>用户Token</returns>
        public ResultModel<string> Post(LoginParam loginInfo)
        {
            string token;
            string authType;
            TokenHelper.TryGetToken(tokenProv, this.Request, out token, out authType);

            ResultState state;

            if (loginInfo != null)
            {
                state = tokenProv.TryRenewToken(loginInfo.Usr, loginInfo.Pwd, loginInfo.ClientId, this.Request.Headers.UserAgent.ToString(), ref token);
            }
            else
            {
                token = null;
                state = ResultState.INV_ARGS;
            }

            var res = new ResultModel<string>()
            {
                State = state,
                Data = token
            };
            if (state >= 0)
            {
                TokenHelper.TrySetToken(tokenProv, token, 0);
            }
            else
            {
                TokenHelper.TryClearToken(tokenProv);
            }
            
            return res;
        }

        /// <summary>
        /// Test, 获取登录信息
        /// </summary>
        /// <returns>表单信息</returns>
        public ResultModel<UserInfo> Get()
        {
            ResultModel<UserInfo> res;
            string token;
            string authType;
            if (TokenHelper.TryGetToken(tokenProv, this.Request, out token, out authType))
            {
                UserInfo userInfo;
                ResultState state = tokenProv.GetUser(token, authType, out userInfo);
                res = new ResultModel<UserInfo>()
                {
                    State = state,
                    Data = userInfo
                };
            }
            else
            {
                res = new ResultModel<UserInfo>()
                {
                    State = ResultState.NOT_LOGIN
                };
            }
            return res;
        }
        public string Get(string id)
        {
            return id;
        }

        // DELETE api/token/
        public ResultModel Delete()
        {
            ResultState res;
            string token;
            string authType;
            if (TokenHelper.TryGetToken(tokenProv, this.Request, out token, out authType))
            {
                res = tokenProv.InvalidToken(token);
                TokenHelper.TryClearToken(tokenProv);
            }
            else
            {
                res = ResultState.SUCCESS;//未登录
            }
            //注销, 使之前的id失效
            return new ResultModel()
            {
                State = res
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tokenProv.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
