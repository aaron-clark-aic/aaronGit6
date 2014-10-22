using WebApiSSO.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebApiSSO.BLL.Token
{
    public interface ITokenProvider : IDisposable
    {
        //实现此接口需要处理传入usr, pwd, token无效或为空的情况
        ResultState TryRenewToken(string usr, string pwd, byte from, string userAgent, ref string token);
        
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        ResultState AddUser(string username, string password);

        /// <summary>
        /// (外部需要验证)重设密码
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="newPwd">新密码</param>
        /// <returns></returns>
        ResultState ResetPwd(string username, string newPwd);

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="usr">用户认证信息</param>
        /// <param name="oldPwd">旧密码</param>
        /// <param name="newPwd">新密码</param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: PASS_ERR, NO_USER, DB_ERR, NOT_LOGIN, SUCCESS</returns>
        ResultState ModifyPwd(Identity usr, string oldPwd, string newPwd);

        [Obsolete("请使用该函数的其它重载")]
        UserInfo GetUser(string token, string authType);

        ResultState GetUser(string token, string authType, out UserInfo userInfo);
        /// <summary>
        /// 用于注销后让之前的token失效
        /// </summary>
        /// <param name="token"></param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: DB_ERR, SUCCESS</returns>
        ResultState InvalidToken(string token);
        #region protected
        ResultState VfyPwd(string usr, string pwd, string token, out int uid);
        ResultState InvalidAllOtherToken(Identity usr);
        ResultState InvalidAllToken(int uid);
        #endregion

        bool IsDisposed { get; }
    }
}
