using WebApiSSO.BLL;
using WebApiSSO.BLL.Token;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http;

namespace WebApiSSO.Api.Models.Utils
{
    /// <summary>
    /// 用于从http上下文中获取和设置Token信息的工具类, 可作为业务层ITokenProvider的扩展函数
    /// </summary>
    public static class TokenHelper
    {
        private const string COOKIE_NAME = "TOKEN";

        /// <summary>
        /// 
        /// 注意: 在异步方法或OWIN中使用此版本可能不支持Cookie
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <param name="authType"></param>
        /// <returns></returns>
        public static bool TryGetToken(this ITokenProvider provider, HttpRequestMessage request, out string token, out string authType)
        {
            return TryGetToken(provider, request, out token, out authType, HttpContext.Current);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <param name="authType"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static bool TryGetToken(this ITokenProvider provider, HttpRequestMessage request, out string token, out string authType, HttpContext httpContext)
        {
            authType = null;
            token = null;
            byte authVal = 0;

            //优先级: userInfo(4) > CAuth(3) > Cookie(2) > Basic(1)
            if (httpContext != null)
            {
                if (authVal < 4)
                {
                    var userInfo = httpContext.User as UserInfo;
                    if (userInfo != null)
                    {
                        token = userInfo.Identity.Token;//从已有用户信息中直接获取
                        authType = userInfo.Identity.AuthenticationType;
                        authVal = 4;
                    }
                }
#if false
            //尽量不依赖httpContext
                if (authVal < 2)
                {
                    var cookies = httpContext.Request.Cookies;
                    if (cookies.AllKeys.Contains(COOKIE_NAME))
                    {
                        //XXX:Get时没有会创建一个新的;;;所以先判断下是否存在
                        var cookie = cookies.Get(COOKIE_NAME);
                        token = cookie.Value;
                        authType = "Cookie";
                    }
                }
#endif
            }
#if true
            if (authVal < 4)
            {
                //尝试从当前线程中读取(对异步无效, 和HttpContext有效范围一致), 是否考虑从外部传入
                var userInfo = Thread.CurrentPrincipal as UserInfo;
                if (userInfo != null)
                {
                    token = userInfo.Identity.Token;//从已有用户信息中直接获取
                    authType = userInfo.Identity.AuthenticationType;
                    authVal = 4;
                }
            }
            if (authVal < 2)
            {
                //XXX: 有多个时是按什么顺序的, 微软的奇葩实现啊...
                var cookies = request.Headers.GetCookies(COOKIE_NAME).FirstOrDefault();
                if (cookies != null)
                {
                    //GetCookies以保证其中一定存在具有指定key的项, 不用担心会被创建一个新的
                    var cookie = cookies[COOKIE_NAME];
                    token = cookie.Value;
                    authType = "Cookie";
                }
            }
#endif

            var authInfo = request.Headers.Authorization;
            if (authInfo != null)
            {
                if (authVal < 3 && "CAuth".Equals(authInfo.Scheme, StringComparison.OrdinalIgnoreCase))
                {
                    token = authInfo.Parameter;
                    authType = "CAuth";
                }
                else if (authVal < 1 && "Basic".Equals(authInfo.Scheme, StringComparison.OrdinalIgnoreCase))
                {
                    //fallback, 测试用
                    string[] infos = Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter)).Split(':');
                    provider.TryRenewToken(infos[0], infos[1], (byte)4, request.Headers.UserAgent.ToString(), ref token);//获取一个鉴权
                    authType = "Basic";
                }
            }

            return authType != null;
        }
        /// <summary>
        /// 
        /// Cookies
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static bool TryClearToken(this ITokenProvider provider)
        {
            return TryClearToken(provider, HttpContext.Current);
        }
        /// <summary>
        /// 
        /// Cookies
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool TryClearToken(this ITokenProvider provider, HttpContext httpContext)
        {
            ClearCookie(httpContext);
            return true;
        }

        /// <summary>
        /// 
        /// Cookies
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static bool TrySetToken(this ITokenProvider provider, string token, int expiredDays)
        {
            return TrySetToken(provider, token, expiredDays, HttpContext.Current);
        }
        /// <summary>
        /// 
        /// Cookies
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="token"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static bool TrySetToken(this ITokenProvider provider, string token, int expiredDays, HttpContext httpContext)
        {
            if (!String.IsNullOrEmpty(token))
            {
                SetCookie(httpContext, token, expiredDays);
            }
            else
            {
                ClearCookie(httpContext);
            }
            return true;
        }
        /// <summary>
        /// 仅在userInfo存在时对其进行设置
        /// </summary>
        /// <param name="provider">仅方便作为扩展函数使用, 无实际用途</param>
        /// <param name="userInfo">用户信息</param>
        public static bool TrySetUserInfo(this ITokenProvider provider, UserInfo userInfo)
        {
            return TrySetUserInfo(provider, userInfo, HttpContext.Current);
        }
        /// <summary>
        /// 仅在userInfo存在时对其进行设置
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="userInfo"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static bool TrySetUserInfo(this ITokenProvider provider, UserInfo userInfo, HttpContext httpContext)
        {
            if (userInfo != null)
            {
                Thread.CurrentPrincipal = userInfo;
                if (httpContext != null)
                {
                    httpContext.User = userInfo;
                }
            }
            return userInfo != null;
        }


        #region cookie
        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="token"></param>
        /// <param name="expiredDays">大于0的过期时间表示天数, 小于等于0则Cookie不设置过期(浏览器会话)</param>
        private static void SetCookie(HttpContext httpContext, string token, int expiredDays)
        {
            HttpCookie cookie;
            cookie = new HttpCookie(COOKIE_NAME)
            {
                Value = Uri.EscapeDataString(token),
                HttpOnly = true,
                //Secure = true//仅https有效
            };
            if (expiredDays > 0)
                cookie.Expires = DateTime.Now.AddDays(expiredDays);
            SetCookie(httpContext, cookie);
        }
        private static void SetCookie(HttpContext httpContext, HttpCookie cookie)
        {
#if false
            this.Request.Headers.TryAddWithoutValidation("Set-Cookie", cookie.ToString());
#else
            //尽量不使用HttpContext
            if (httpContext != null)
            {
                httpContext.Response.Cookies.Add(cookie);
            }
#endif
        }

        private static void ClearCookie(HttpContext httpContext)
        {
            var cookie = new HttpCookie(COOKIE_NAME)
            {
                Value = "",
                Expires = DateTime.Now.AddYears(-1),
                HttpOnly = true
            };
            SetCookie(httpContext, cookie);
        }
        #endregion

        public static ResultState GetUserInfo(this ITokenProvider provider, HttpRequestMessage request, HttpContext httpContext, out UserInfo ui)
        {
            ResultState state;
            string token = null;
            string scheme = null;

            if (TokenHelper.TryGetToken(provider, request, out token, out scheme, httpContext))
            {
                state = provider.GetUser(token, scheme, out ui);
            }
            else
            {
                ui = null;
                state = ResultState.NOT_LOGIN;
            }
            return state;
        }

        public static UserInfo GetUserInfo(this ApiController ctl, HttpContext context)
        {
            UserInfo ui = (ctl.User as UserInfo);
            if (ui == null)
            {
                using(ITokenProvider provider = new TokenProvider())
                {
                    ResultState state = TokenHelper.GetUserInfo(provider, ctl.Request, context, out ui);
                }
            }
            return ui;
        }
        public static UserInfo GetUserInfo(this ApiController ctl)
        {
            return TokenHelper.GetUserInfo(ctl, HttpContext.Current);
        }

        public static Identity GetUserIdentity(this ApiController ctl)
        {
            var ui = TokenHelper.GetUserInfo(ctl);
            return (ui != null) ? ui.Identity : null;
        }
    }
}