using WebApiSSO.Api.Models;
using WebApiSSO.Api.Models.Utils;
using WebApiSSO.BLL;
using WebApiSSO.BLL.Utils;
using WebApiSSO.BLL.Token;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace WebApiSSO.Api.Filters
{
    public class CAuthorizeAttribute : AuthorizeAttribute
    {

        private static string REMOTE_LOGIN = JsonConvert.SerializeObject(new ResultModel() { State = ResultState.REMOTE_LOGIN });

        private static string NOT_LOGIN = JsonConvert.SerializeObject(new ResultModel() { State = ResultState.NOT_LOGIN });
        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            //var challengeMessage = actionContext.Request.CreateResponse(System.Net.HttpStatusCode.Unauthorized, res);
            var challengeMessage = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            //challengeMessage.Headers.Add("WWW-Authenticate", "CC Realm=\"CAuth\"");
            challengeMessage.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("CAuth", "Realm=\"CAuth\""));
            challengeMessage.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Basic", "Realm=\"CAuth\""));//fallback, 测试用
            challengeMessage.Content = new StringContent(NOT_LOGIN, Encoding.UTF8, "application/json");

            actionContext.Response = challengeMessage;
            //throw new System.Web.Http.HttpResponseException(challengeMessage);
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            bool authed = false;

            using (ITokenProvider provider = new TokenProvider())
            {
                UserInfo userInfo;
                var httpContext = HttpContext.Current;//最好不要用
                ResultState state = TokenHelper.GetUserInfo(provider, actionContext.Request, httpContext, out userInfo);
                if (state.IsSuccess())
                {
                    authed = TokenHelper.TrySetUserInfo(provider, userInfo, httpContext);
                    if (!userInfo.Identity.Enable)
                    {
                        //返回异地登录(注意authed==true, 否则会返回为登录)
                        //actionContext.Response = actionContext.Request.CreateResponse(System.Net.HttpStatusCode.OK, res);
                        actionContext.Response = new HttpResponseMessage() { Content = new StringContent(REMOTE_LOGIN, Encoding.UTF8, "application/json") };
                    }
                }
            }
            return authed;
        }
    }
}