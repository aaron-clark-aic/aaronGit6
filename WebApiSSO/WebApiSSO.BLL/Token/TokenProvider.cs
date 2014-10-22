using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using EntityFramework.Extensions;
using CH.Home.DAL;
using CH.Home.BLL.Utils;
using System.Diagnostics;
using System.IO;
using CH.Home.BLL.Business;


namespace WebApiSSO.BLL.Token
{
    public class TokenProvider : BaseBusiness, ITokenProvider, IDisposable
    {
        #region 业务统一构造函数
        public TokenProvider() : base() { }
        public TokenProvider(DbSession session) : base(session) { }
        #endregion

        public ResultState TryRenewToken(string usr, string pwd, byte from, string userAgent, ref string token)//userAgent将无效
        {
            int sid = rand.Next();
            //Trace.TraceInformation(String.Format("{5}: TryRenewToken({0}, {1}, {2}, {3}, {4})\n", usr, pwd, from, userAgent, token, sid));
            //token = null;
            int uid;
            ResultState state = VfyPwd(usr, pwd, token, out uid);
            if (state.IsSuccess())
            {
                try
                {
                    //多点登录的限制
                    var tokens = DbContext.UserTokens.Where((t) => t.UserId == uid && t.ClientId == from);
                    Guid guid;
                    int tokenUid;
                    bool onlyOneClient = (from == 1);
                    bool noToken = true;//标记有没有找到对应的token
                    if (TryParseGuid(out guid, out tokenUid, token) && uid == tokenUid)
                    {
                        //Trace.TraceInformation("{2}: Guid OK -> {0}({1})\n", guid, uid, sid);
                        if (onlyOneClient)
                        {
                            try
                            {
                                //之前存在有效的token, 使此用户来源的其它token异地登陆
                                tokens.Where(t => t.Token != guid).Update(t => new UserToken()
                                {
                                    Enabled = false
                                });
                            }
                            catch { }
                        }
                        var tmp = tokens.Where(t => t.Token == guid).FirstOrDefault();
                        if (tmp != null)
                        {
                            //之前的token是当前用户的, 可以继续使用
                            tmp.Enabled = true;
                            tmp.LastTime = DateTime.Now;
                            noToken = false;
                        }
                        else
                        {
                            //未找到之前的token数据, 将生成一个新的
                        }

                    }
                    else
                    {
                        if (onlyOneClient)
                        {
                            try
                            {
                                //之前没有有效的token,, 使此用户来源的所有token异地登陆
                                tokens.Update(t => new UserToken()
                                {
                                    Enabled = false
                                });
                            }
                            catch { }
                        }
                    }
                    if (noToken)
                    {
                        //生成一个新的token
                        var tokenObj = new UserToken()
                        {
                            UserId = uid,
                            LastTime = DateTime.Now,
                            ClientId = from,
                            ClientName = userAgent,
                            Enabled = true
                        };
                        DbContext.UserTokens.Add(tokenObj);
                        DbContext.SaveChanges();
                        //必须在SaveChanges之后Token才有值
                        token = FormatGuid(tokenObj.UserId, tokenObj.Token);
                    }
                    else
                    {
                        DbContext.SaveChanges();
                    }
                    //Trace.TraceInformation("{1}: Get {2}, success!!! -> {0}\n", token, sid, noToken ? "new" : "old");
                    state = ResultState.SUCCESS;
                }
                catch
                {
                    state = ResultState.DB_ERR;
                }
            }
            return state;
        }

        public ResultState VfyPwd(string usr, string pwd, string token, out int uid)
        {
            uid = 0;
            if (!String.IsNullOrEmpty(usr) && !String.IsNullOrEmpty(pwd))
            {
                try
                {
                    var userInfo = DbContext.Users
                        .Where((user) => user.Name == usr)
                        .Select(u => new { ID = u.ID, Name = u.Name, Passwd = u.Passwd })
                        .FirstOrDefault();

                    if (userInfo != null)
                    {
                        if (userInfo.Passwd == pwd)
                        {
                            //TODO: 哈希
                            //密码验证通过
                            uid = userInfo.ID;
                            return ResultState.SUCCESS;
                        }
                        else
                        {
                            //密码错误
                            return ResultState.PASS_ERR;
                        }
                    }
                    else
                    {
                        //用户不存在
                        return ResultState.NO_USER;
                    }
                }
                catch
                {
                    return ResultState.DB_ERR;
                }
            }
            else
            {
                //TODO: 单纯根据Token获取用户信息
                //用户不存在
                return ResultState.NO_USER;
            }
        }

        private static object a_lock = new object();
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        public ResultState AddUser(string username, string password)
        {
            ResultState state;
            try
            {
#if DEBUG
                #region 谭欣要加的测试账号
                if (username == "12345678912")
                {
                    var u = DbContext.Users.FirstOrDefault(i => i.Name == username);
                    if (u != null)
                    {
                        u.Passwd = password;
                    }
                    else
                    {
                        DbContext.Users.Add(new User()
                        {
                            Name = username,
                            Passwd = password,
                            RegTime = DateTime.Now //写入注册时间
                        });
                    }
                    DbContext.SaveChanges();
                    state = ResultState.SUCCESS;
                }
                #endregion
#endif
                //上个锁，保证线程安全
                lock (a_lock)
                {
                    //验证用户是否存在
                    state = Session.GetBusiness<AccountBusiness>().CheckUsername(username);
                    if (state == ResultState.NO_USER)
                    {
                        DbContext.Users.Add(new User()
                        {
                            //TODO: 哈希
                            Name = username,
                            Passwd = password,
                            RegTime = DateTime.Now //写入注册时间
                        });
                        DbContext.SaveChanges();
                        state = ResultState.SUCCESS;
                    }
                }
            }
            catch
            {
                state = ResultState.DB_ERR;
            }
            return state;
        }

        /// <summary>
        /// (外部需要验证)重设密码
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="newPwd">新密码</param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: INV_ARGS(NO_USER), DB_ERR, SUCCESS</returns>
        public ResultState ResetPwd(string username, string newPwd)
        {
            ResultState state;
            try
            {
                var me = DbContext.Users.Where(i => i.Name == username).FirstOrDefault();
                if (me != null)
                {
                    //TODO: 哈希
                    me.Passwd = newPwd;
                    DbContext.SaveChanges();
                    //失效之前的所有Token
                    InvalidAllToken(me.ID);
                    state = ResultState.SUCCESS;
                }
                else
                {
                    state = ResultState.INV_ARGS;//FIXME: NO_USER
                }
            }
            catch
            {
                state = ResultState.DB_ERR;
            }
            return state;
        }
        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="usr">用户认证信息</param>
        /// <param name="oldPwd">旧密码</param>
        /// <param name="newPwd">新密码</param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: PASS_ERR, NO_USER, DB_ERR, NOT_LOGIN, SUCCESS</returns>
        public ResultState ModifyPwd(Identity usr, string oldPwd, string newPwd)
        {
            ResultState state = CheckLogin(usr);
            if (state.IsSuccess())
            {
                try
                {
                    var me = DbContext.Users.Where(u => u.ID == usr.Id).FirstOrDefault();
                    if (me != null)
                    {
                        if (oldPwd == me.Passwd)
                        {
                            //TODO: 哈希
                            me.Passwd = newPwd;
                            DbContext.SaveChanges();
                            //失效之前的所有Token
                            InvalidAllOtherToken(usr);
                            state = ResultState.SUCCESS;
                        }
                        else
                        {
                            state = ResultState.PASS_ERR;
                        }
                    }
                    else
                    {
                        state = ResultState.NO_USER;
                    }
                }
                catch
                {
                    state = ResultState.DB_ERR;
                }
            }
            return state;
        }

        /// <summary>
        /// 根据Token从数据库获取用户信息
        /// </summary>
        /// <param name="token"></param>
        /// <param name="authType"></param>
        /// <param name="userInfo">(输出)用户信息, 当且仅当失败返回null</param>
        /// <returns></returns>
        public ResultState GetUser(string token, string authType, out UserInfo userInfo)
        {
            ResultState state;
            userInfo = null;
            Guid guid;
            int uid;
            if (TryParseGuid(out guid, out uid, token))
            {
                //DateTime now = DateTime.Now;
                try
                {
                    //TODO: 验证异地登录
                    var tokenInfo = DbContext.UserTokens
                        .Where((t) => t.Token == guid)
                        .Select(t => new { t.Enabled, t.UserId, t.User.Name, t.User.NickName })
                        .FirstOrDefault();
                    if (tokenInfo != null)//验证有效性
                    {
                        if (tokenInfo.UserId == uid)
                        {
#if false
                            userInfo = DbContext.Users.Where(u => u.ID == uid).Select((u) => new UserInfo()
                            {
                                Identity = new Identity()
                                {
                                    AuthenticationType = authType,
                                    Id = u.ID,
                                    Name = u.Name,
                                    Token = token,
                                    Enable = tokenInfo.Enabled,
                                    NickName = String.IsNullOrEmpty(u.NickName) ? u.Name : u.NickName
                                }
                            }).FirstOrDefault();
                            state = (userInfo != null) ? ResultState.SUCCESS : ResultState.NO_USER;//按照数据库关系不可能出现NO_USER的情况
#else
                            userInfo = new UserInfo()
                            {
                                Identity = new Identity()
                                {
                                    AuthenticationType = authType,
                                    Id = tokenInfo.UserId,
                                    Name = tokenInfo.Name,
                                    Token = token,
                                    Enable = tokenInfo.Enabled,
                                    NickName = String.IsNullOrEmpty(tokenInfo.NickName) ? tokenInfo.Name : tokenInfo.NickName
                                }
                            };
                            state = ResultState.SUCCESS;
#endif
                        }
                        else
                        {
                            //Token与数据库中用户ID不匹配
                            state = ResultState.INV_ARGS;
                        }
                    }
                    else
                    {
                        //Token在数据库中不存在
                        state = ResultState.NOT_LOGIN;
                    }
                }
                catch
                {
                    //数据库错误
                    state = ResultState.DB_ERR;
                }
            }
            else
            {
                //无或非法Token
                state = ResultState.INV_ARGS;
            }

            return state;
        }

        /// <summary>
        /// 根据Token从数据库获取用户信息
        /// </summary>
        /// <param name="token"></param>
        /// <param name="authType"></param>
        /// <returns>失败返回null</returns>
        [Obsolete("请使用该函数的其它重载")]
        public UserInfo GetUser(string token, string authType)
        {
            UserInfo userInfo;
            GetUser(token, authType, out userInfo);
            return userInfo;
        }
        /// <summary>
        /// 用于(修改密码后)让指定用户之前的其它token全部失效
        /// </summary>
        /// <param name="usr">用户认证信息</param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: DB_ERR, NOT_LOGIN, SUCCESS</returns>
        public ResultState InvalidAllOtherToken(Identity usr)
        {
            ResultState state = CheckLogin(usr);
            if (state.IsSuccess())
            {
                try
                {
                    Guid guid;
                    int uid;
                    if (TryParseGuid(out guid, out uid, usr.Token))
                    {
                        DbContext.UserTokens.Where(t => t.UserId == uid && t.Token != guid).Delete();
                        state = ResultState.SUCCESS;
                    }
                    else
                    {
                        state = ResultState.NOT_LOGIN;//??, 非正常流程
                    }
                }
                catch
                {
                    state = ResultState.DB_ERR;
                }
            }
            return state;
        }
        /// <summary>
        /// (内部)用于(重置密码后)让指定用户之前的token全部失效
        /// </summary>
        /// <param name="uid">用户ID</param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: DB_ERR, SUCCESS</returns>
        public ResultState InvalidAllToken(int uid)
        {
            ResultState state;
            try
            {
                DbContext.UserTokens.Where(t => t.UserId == uid).Delete();
                state = ResultState.SUCCESS;
            }
            catch
            {
                state = ResultState.DB_ERR;
            }
            return state;
        }
        /// <summary>
        /// 用于注销后让之前的token失效
        /// </summary>
        /// <param name="token">用户Token</param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: DB_ERR, SUCCESS</returns>
        public ResultState InvalidToken(string token)
        {
            ResultState state;
            Guid guid;
            int uid;
            if (TryParseGuid(out guid, out uid, token))
            {
                try
                {
                    var val = DbContext.UserTokens.Where((t) => t.Token == guid);
#if false
                    DbContext.Table_UserToken.Remove(val.FirstOrDefault());
                    DbContext.SaveChanges();
#else
                    //引入EntityFramework.Extended(EntityFramework.Extensions)
                    val.Delete();
#endif
                    state = ResultState.SUCCESS;
                }
                catch
                {
                    state = ResultState.DB_ERR;
                }
            }
            else
            {
                state = ResultState.SUCCESS;//旧有Token无效
            }
            return state;
        }

        #region guid转字符串(加一位盐和一位校验)

        private static Random rand = new Random();
        private const byte CHECK_VAL = 0x4D;

        private bool TryParseGuid(out Guid guid, out int uid, string token)
        {
            guid = default(Guid);
            uid = 0;
            bool res = false;
            if (!String.IsNullOrEmpty(token))
            {
                try
                {
                    byte[] val = Convert.FromBase64String(token.Replace('-', '+').Replace('_', '/').Replace('.','='));
                    if (val.Length == 22)
                    {
                        byte check = val[19];
                        check ^= val[21];

                        if (check == CHECK_VAL)
                        {
                            //校验通过
                            for (int i = 18; i >= 0; --i)
                            {
                                val[i + 1] ^= val[i];//逐位异或
                            }
                            val[0] ^= val[20];//盐

                            uid = BitConverter.ToInt32(val, 16);
                            Array.Resize(ref val, 16);
                            guid = new Guid(val);
                            res = true;
                        }
                    }
                }
                catch { }
            }
            return res;
        }

        private string FormatGuid(int uid, Guid guid)
        {
            byte[] val = guid.ToByteArray();
            Array.Resize(ref val, 22);

            Buffer.BlockCopy(BitConverter.GetBytes(uid), 0, val, 16, 4);//将uid复制到16~19

            //加盐
            //加盐并不能阻止顺序guid的尾数的一致, 只是能让其分布情况更加复杂, 不易看出
            byte check = (byte)rand.Next(256);//(byte)MersenneTwister.Next(256), (byte)(DateTime.UtcNow.Ticks & 0x000000ff)  1E8 times -> 1372ms, 2527ms, 1716ms
            val[20] = check;
            for (int i = 0; i <= 19; ++i)
            {
                check = (val[i] ^= check);//逐位异或
            }
            //生成校验码
            val[21] = CHECK_VAL;
            val[21] ^= check;
            return Convert.ToBase64String(val).Replace('+','-').Replace('/','_').Replace('=', '.');//无需专门编码
        }
        #endregion

        #region IDisposable Members

        /// <summary>
        /// Internal variable which checks if Dispose has already been called
        /// </summary>
        private Boolean disposed;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(Boolean disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                //Managed cleanup code here, while managed refs still valid
                DbContext.Dispose();
            }
            //Unmanaged cleanup code here

            disposed = true;
        }


        #endregion
    }
}