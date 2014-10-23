using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Collections;
using System.Timers;
using WebApiSSO.DAL;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using WebApiSSO.BLL.Token;
using WebApiSSO.BLL.Utils;
using EntityFramework.Extensions;

namespace WebApiSSO.BLL.Business
{
    #region 业务所需类型
    /// <summary>
    /// 验证码类型(>=0 为业务手动删除，<0为验证时自动删除)
    /// </summary>
    public enum VerifyCodeType
    {
        REGIST = 0, //注册用
        C_PASS = 1 //修改密码用
    }

    /// <summary>
    /// 验证码类
    /// </summary>
    class VerifyCode
    {
        /// <summary>
        /// 用户名（手机号）
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public VerifyCodeType Type { get; set; }

        /// <summary>
        /// 验证码值
        /// </summary>
        public string CodeValue { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime DaedTime { get; set; }
    }

    /// <summary>
    /// 验证码字典
    /// </summary>
    class CodeDic
    {
        //系统自动清理验证码是否过期的循环时间
        private static readonly double interval;
        //验证码过期时间(分钟)
        public static readonly int OutTime;
        //线程安全的验证码集合
        static ConcurrentDictionary<string, VerifyCode> Code = null;

        static readonly object cl = new object();

        static CodeDic()
        {
            OutTime = 5;//5分钟
            interval = 1000 * 60 * OutTime;
        }
        //初始化dic并启动清理计时器
        public static ConcurrentDictionary<string, VerifyCode> GetInstance()
        {
            if (Code == null)
            {
                lock (cl)
                {
                    if (Code == null)
                    {
                        Code = new ConcurrentDictionary<string, VerifyCode>();
                        Timer Timer = new Timer(interval);
                        Timer.Elapsed += (object sender, ElapsedEventArgs e) =>
                        {
                            DateTime now = DateTime.Now;
                            foreach (var o in Code)
                            {
                                if (o.Value.DaedTime < now)
                                {
                                    //确保此期间值未被改变
                                    ((IDictionary<string, VerifyCode>)Code).Remove(o);
                                }
                            }
                        };
                        Timer.AutoReset = true;
                        Timer.Start();
                    }
                }
            }
            return Code;
        }
    }

    /// <summary>
    /// 用户基本信息类
    /// </summary>
    public class BaseUser : UserUpateInfo
    {
        public string Name { get; set; }
        public string PhotoUrl { get; set; }
        public Nullable<System.DateTime> RegTime { get; set; }
        public Nullable<System.DateTime> LoginTime { get; set; }
        public Nullable<int> Level { get; set; }
        public Nullable<int> LoginCount { get; set; }
    }

    /// <summary>
    /// 用户可更改信息类
    /// </summary>
    public class UserUpateInfo
    {
        public int ID { get; set; }
        public Nullable<bool> Gender { get; set; }
        public Nullable<DateTime> Birthday { get; set; }
        public Nullable<decimal> Height { get; set; }
        public Nullable<decimal> Weight { get; set; }
        public string Area { get; set; }
        public string NickName { get; set; }
        public string Sign { get; set; }
        public byte? Status { get; set; }
        public short? Type { get; set; }
    }

    #endregion

    #region 参数

    /// <summary>
    /// 财富的获取方式
    /// </summary>
    public enum UserBalanceType : short
    {
        /// <summary>
        /// 签到获取
        /// </summary>
        SignIn = 0
    }

    /// <summary>
    /// 财富的类型值
    /// </summary>
    public enum BalanceTypeValue
    {
        SignIn = 50
    }

    /// <summary>
    /// 获取财富参数
    /// </summary>
    public class UserBalanceParam
    {
        /// <summary>
        /// 家庭ID
        /// </summary>
        public int? FamilyID { get; set; }

        //public int? Amount { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public UserBalanceType Type { get; set; }
    }
    #endregion

    #region 账户业务
    /// <summary>
    /// 账户业务
    /// </summary>
    public class AccountBusiness : BaseBusiness
    {
        public AccountBusiness() : base() { }
        public AccountBusiness(DbSession session) : base(session) { }


        #region 验证用户名
        /// <summary>
        /// 验证用户名是否存在
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns></returns>
        internal ResultState CheckUsername(string username)
        {
            try
            {
                return DbContext.User.Where(i => i.User_Name == username).Any() ? ResultState.HAS_USER : ResultState.NO_USER;
            }
            catch
            {
                return ResultState.DB_ERR;
            }
        }
        #endregion

        #region 初始化用户密码
        /// <summary>
        /// 初始化用户密码
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="codeValue">验证码</param>
        /// <returns></returns>
        public ResultState SetPassword(string username, string password, string codeValue)
        {
#if DEBUG==flase
            #region 谭欣要加的测试账号
            if (username.Equals("12345678912"))
            {
                var tu = DbContext.User.Where(i => i.Name == username).FirstOrDefault();
                if (tu == null)
                {
                    DbContext.User.Add(new User()
                    {
                        Name = username,
                        Passwd = password
                    });
                }
                else
                {
                    tu.Passwd = password;
                }
                return ResultState.SUCCESS;
            }
            #endregion
#endif
            //验证验证码并强制删除
            //var c_r = CheckMsgVerifyCode(username, codeValue, VerifyCodeType.REGIST, true);
            //if (c_r != ResultState.SUCCESS)
            //{
            //    return c_r;
            //}

            var tokenProvider = Session.GetITokenProvider();
            return tokenProvider.AddUser(username, password);

        }
        #endregion

        #region 找回修改用户密码
        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="codeValue">验证码</param>
        /// <returns></returns>
        public ResultState ChangePassword(string username, string password, string codeValue)
        {
            //验证验证码并强制删除
            //var c_r = CheckMsgVerifyCode(username, codeValue, VerifyCodeType.C_PASS, true);
            //if (c_r != ResultState.SUCCESS)
            //{
            //    return c_r;
            //}
            var tokenProvider = Session.GetITokenProvider();
            return tokenProvider.ResetPwd(username, password);
        }
        #endregion

        #region 利用旧密码修改用户密码
        public ResultState SetNewPassword(string username, string oPassword, string nPassword)
        {
            try
            {
                var r = DbContext.User.Where(u => u.User_Name == username && u.User_Passwd == oPassword);
                if (r.Count() == 0)
                {
                    return ResultState.PASS_ERR;
                }

                var tokenProvider = Session.GetITokenProvider();
                return tokenProvider.ResetPwd(username, nPassword);
            }
            catch
            {
                return ResultState.DB_ERR;
            }
        }
        #endregion

        public ResultState PurgeUser(Identity usr, string usrName, string usrPass)
        {
            ResultState state = CheckLogin(usr);
            try
            {
                if (state.IsSuccess())
                {
                    //NOTE: 将不能产生消息
                    var usrInfo = DbContext.User.Where(u => u.User_ID == usr.Id).FirstOrDefault();
                    if (usrInfo != null)
                    {
                        if (usrInfo.User_Name == usrName && usrInfo.User_Passwd == usrPass)
                        {
                            int uid = usr.Id;                         
                           
                            DbContext.UserToken.RemoveRange(usrInfo.User_Token);
                            //移除自身
                            DbContext.User.Remove(usrInfo);
                            //保存
                            DbContext.SaveChanges();
                            state = ResultState.SUCCESS;
                        }
                        else
                        {
                            state = ResultState.PERM_DENY;
                        }
                    }
                    else
                    {
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
      
    }
    #endregion
}

