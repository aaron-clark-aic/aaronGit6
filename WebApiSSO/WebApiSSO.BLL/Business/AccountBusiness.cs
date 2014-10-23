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

        #region 获取短信验证码
        /// <summary>
        /// 用户获取短信验证码
        /// </summary>
        /// <param name="username">用户名（手机号）</param>
        /// <param name="type">用户名（手机号）</param>
        /// <returns></returns>
        public ResultState RuqestMsgVerifyCode(string username, VerifyCodeType type)
        {
            try
            {
                var c_u = CheckUsername(username);
                switch (type)
                {
                    case VerifyCodeType.C_PASS:
                        if (c_u != ResultState.HAS_USER)//如果用户不存在则返回错误信息
                        {
                            return c_u;
                        }
                        break;
                    case VerifyCodeType.REGIST:
#if DEBUG
                        #region 谭欣要加的测试账号
                        if (username.Equals("12345678912"))
                        {
                            string _code = "123456";
                            AddToDicionary(username, _code, type);
                            return ResultState.SUCCESS;
                        }
                        #endregion
#endif
                        if (c_u == ResultState.HAS_USER)//如果用户存在则返回错误信息
                        {
                            return c_u;
                        }
                        break;
                    default:
                        return ResultState.INV_ARGS;
                }
                string code = GenerateCode(6);//生成验证码
                string msg = new StringBuilder().Append(string.Format("您的验证码是{0}请在{1}分钟内使用，否则将失效", code, 5)).ToString();
                var msgResult = MsgProvider.getInstance().SendSMS(username, msg);//发送验证码
                if (msgResult.ReturnStatus == 0)
                {
                    //value = code;
                    AddToDicionary(username, code, type);
                    return ResultState.SUCCESS;
                }
                else
                {
                    return ResultState.SEND_VFR_ERRO;
                }
            }
            catch
            {
                return ResultState.UNKNOWN_ERR;
            }
        }

        //将验证码写入集合
        void AddToDicionary(string username, string code, VerifyCodeType type)
        {
            DateTime deadTime = DateTime.Now + new TimeSpan(0, CodeDic.OutTime, 0);//5分钟后过期
            VerifyCode vCode = new VerifyCode()
            {
                Username = username,
                CodeValue = code,
                Type = type,
                DaedTime = deadTime
            };
            //键值名后加上类型
            string w_name = username + "_" + ((int)type).ToString();
            CodeDic.GetInstance()[w_name] = vCode;
        }

        /// 随机生成验证码（数字）
        private string GenerateCode(int codeL)
        {
            string code = string.Empty;
            Random r = new Random();
            for (int i = 0; i < codeL; i++)
            {
                code += r.Next(10);
            }
            return code;
        }
        #endregion

        #region 验证短信验证码
        /// <summary>
        /// 验证短信验证码
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="codeValue">验证码</param>
        /// <param name="type">类型</param>
        /// <param name="forceaDelete">是否强制删除</param>
        /// <returns></returns>
        public ResultState CheckMsgVerifyCode(string username, string codeValue, VerifyCodeType type, bool forceaDelete = false)
        {
            int token = int.Parse(Enum.Format(type.GetType(), type, "d").ToString());
            //键值名后加上类型
            string w_name = username + "_" + token;
            VerifyCode vCode = null;
            if (CodeDic.GetInstance().TryGetValue(w_name, out vCode))
            {
                //如果存在此验证码,值正确且未过期
                if (vCode != null && vCode.DaedTime >= DateTime.Now && vCode.CodeValue.Equals(codeValue))
                {
                    //小于0自动删除或者指定强制删除
                    if (token < 0 || forceaDelete)
                    {
                        CodeDic.GetInstance().TryRemove(w_name, out vCode);
                    }
                    //写入操作的权限集合
                    //WriteToPermission(username, type);
                    return ResultState.SUCCESS;
                }
                else
                {
                    return ResultState.VFY_ERR;
                }
            }
            else
            {
                return ResultState.DATA_OVERDUE;
            }
        }
        #endregion

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
                return DbContext.Users.Where(i => i.Name == username).Any() ? ResultState.HAS_USER : ResultState.NO_USER;
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
#if DEBUG
            #region 谭欣要加的测试账号
            if (username.Equals("12345678912"))
            {
                var tu = DbContext.Users.Where(i => i.Name == username).FirstOrDefault();
                if (tu == null)
                {
                    DbContext.Users.Add(new User()
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
            var c_r = CheckMsgVerifyCode(username, codeValue, VerifyCodeType.REGIST, true);
            if (c_r != ResultState.SUCCESS)
            {
                return c_r;
            }

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
            var c_r = CheckMsgVerifyCode(username, codeValue, VerifyCodeType.C_PASS, true);
            if (c_r != ResultState.SUCCESS)
            {
                return c_r;
            }

            var tokenProvider = Session.GetITokenProvider();
            return tokenProvider.ResetPwd(username, password);
        }
        #endregion

        #region 利用旧密码修改用户密码
        public ResultState SetNewPassword(string username, string oPassword, string nPassword)
        {
            try
            {
                var r = DbContext.Users.Where(u => u.Name == username && u.Passwd == oPassword);
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

        #region 获取用户信息
        /// <summary>
        /// 根据用户名获取用户信息
        /// </summary>
        /// <param name="usr">用户认证信息</param>
        /// <param name="username"></param>
        /// <returns></returns>
        public ResultState GetUserInfo(Identity usr, string username, out BaseUser userinfo)
        {
            userinfo = null;
            ResultState state = CheckLogin(usr);
            if (state.IsSuccess())
            {

                IQueryable<User> query;
                if (!String.IsNullOrEmpty(username))
                {
                    query = DbContext.Users
                          .Where(u => u.Name == username);
                }
                else
                {
                    query = DbContext.Users
                          .Where(u => u.ID == usr.Id);
                }
                try
                {
                    userinfo = query.Select<User, BaseUser>(m =>
                             new BaseUser()
                             {
                                 Birthday = m.Birthday,
                                 Area = m.Area,
                                 Gender = m.Gender,
                                 Height = m.Height,
                                 Level = m.Level,
                                 Name = m.Name,
                                 PhotoUrl = m.PhotoUrl,
                                 RegTime = m.RegTime,
                                 NickName = m.NickName,
                                 Weight = m.Weight,
                                 Type = m.Type,
                                 ID = m.ID,
                                 Status = m.Status,
                                 Sign = m.Sign,
                                 LoginCount = m.LoginCount,
                                 LoginTime = m.LoginTime
                             }).FirstOrDefault();
                    if (userinfo != null)
                    {
                        state = ResultState.SUCCESS;
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
        #endregion

        #region 更新用户信息
        /// <summary>
        /// 更新一个信息（暂时弃用）
        /// </summary>
        /// <param name="usr">用户认证信息</param>
        /// <param name="attr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [Obsolete("此函数逻辑已不再维护, 请使用UpdateUserInfo代替", true)]
        public ResultState UpdateUserInfo_OneItem(Identity usr, string attr, object value)
        {
            //var entry = DbContext.Entry<User>(user);
            //entry.State = System.Data.Entity.EntityState.Unchanged;
            var user = DbContext.Users.FirstOrDefault(i => i.ID == usr.Id);
            Type u_t = user.GetType();
            PropertyInfo p = u_t.GetProperty(attr);
            if (p == null)
            {
                return ResultState.INV_ARGS;
            }
            Type a_t = p.PropertyType;
            //p.SetValue(user, Convert.ChangeType(value, a_t), null);
            if (!p.PropertyType.IsGenericType)
            {
                //非泛型
                try
                {
                    p.SetValue(user, string.IsNullOrEmpty(value.ToString()) ? null : Convert.ChangeType(value, p.PropertyType), null);
                }
                catch
                {
                    return ResultState.DATA_CONFLICT;
                }
            }
            else
            {
                //泛型Nullable<>
                Type genericTypeDefinition = p.PropertyType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Nullable<>))
                {
                    p.SetValue(user, string.IsNullOrEmpty(value.ToString()) ? null : Convert.ChangeType(value, Nullable.GetUnderlyingType(p.PropertyType)), null);
                }
            }
            if ("NickName" == attr)
            {
                usr.NickName = value.ToString();
            }
            //u_t.InvokeMember(attr,System.Reflection.BindingFlags.SetProperty,null,user,new object[value]);    
            DbContext.SaveChanges();
            return ResultState.SUCCESS;
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="usr">用户认证信息</param>
        /// <param name="n_user"></param>
        /// <returns></returns>
        public ResultState UpdateUserInfo(Identity usr, UserUpateInfo updatedUserInfo)
        {
            //FIXME: 权限验证
            try
            {
                var d_user = DbContext.Users.FirstOrDefault(u => u.ID == updatedUserInfo.ID);
                Type n_type = updatedUserInfo.GetType();
                Type d_type = d_user.GetType();
                var properties = n_type.GetProperties();
                foreach (var p in properties)
                {
                    if (p.Name.ToLower() == "id")//id不赋值
                    {
                        continue;
                    }
                    var value = p.GetValue(updatedUserInfo, null);
                    if (value == null)
                    {
                        continue;
                    }
                    d_type.GetProperty(p.Name).SetValue(d_user, value, null);
                    if ("NickName".Equals(p.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        usr.NickName = value.ToString();
                    }
                }
                DbContext.SaveChanges();
                return ResultState.SUCCESS;
            }
            catch
            {
                return ResultState.DB_ERR;
            }
        }
        #endregion

        #region 写入头像图片地址
        public ResultState WritePhoneUrl(int id, string path, out string oldFileName)
        {
            oldFileName = null;
            try
            {
                var user = DbContext.Users.FirstOrDefault(i => i.ID == id);
                if (user == null)
                {
                    return ResultState.NO_USER;
                }
                oldFileName = user.PhotoUrl;
                user.PhotoUrl = path;
                DbContext.SaveChanges();
                return ResultState.SUCCESS;
            }
            catch
            {
                return ResultState.DB_ERR;
            }
        }
        #endregion

        #region 用户获取财富
        /// <summary>
        /// 
        /// </summary>
        /// <param name="usr"></param>
        /// <param name="signInfo"></param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: PERM_DENY, INV_ARGS, NOT_LOGIN, DB_ERR, SUCCESS, SUCCESS_DUP</returns>
        public ResultState UserGetBalance(Identity usr, UserBalanceParam signInfo)
        {
            decimal amount = 0;
            short type = (short)signInfo.Type;
            switch (signInfo.Type)
            {
                //签到获取财富
                case UserBalanceType.SignIn:
                    amount = (decimal)BalanceTypeValue.SignIn;
                    break;
                default:
                    //无效类型
                    return ResultState.INV_ARGS;
            }
            try
            {
                int fid = (int)signInfo.FamilyID;

                //获取并验证该用户和家庭
                DateTime getTime = DateTime.Today;
                ResultState state = SignState(usr, fid, signInfo.Type, getTime);
                if (state == ResultState.SUCCESS)//忽略SUCCESS_DUP
                {
                    //添加个人财富记录
                    DbContext.Accounts.Add(new Account()
                    {
                        FamilyID = signInfo.FamilyID,
                        Comment = signInfo.Comment,
                        UserID = usr.Id,
                        CheckInType = type,
                        CheckInTime = getTime,
                        Exp = amount
                    });

                    //将本次财富添加到家庭成员财富中
                    var fm = DbContext.FamilyMembers.Include("Family").Where(FamilyBusiness.PrepareCheckFamilyMemberExp(usr.Id, fid)).FirstOrDefault();//
                    fm.Exp += amount;

                    //将本次财富添加到家庭总财富中
                    //var f = DbContext.Families.Where(FamilyBusiness.PrepareCheckFamilyExp(fid)).FirstOrDefault();
                    var f = fm.Family;
                    f.Exp += amount;
                    DbContext.SaveChanges();
                    state = ResultState.SUCCESS;
                }
                return state;
            }
            catch
            {
                return ResultState.DB_ERR;
            }
        }
        //TODO: 查询明细
        #endregion

        #region 模糊查询用户
        /// <summary>
        /// 模糊查询用户
        /// </summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        public ResultState SearchUser(string keyWord, int Index, int count, out List<BaseUser> userList)
        {
            userList = null;
            bool isUsername = false;
            if (Regex.IsMatch(keyWord, @"^\d+$"))
            {
                isUsername = true;
            }
            try
            {
                userList = DbContext.Users
                    .Where(i => keyWord == "" || (isUsername && (i.Name.Contains(keyWord) || (!i.Name.Contains(keyWord) && i.NickName.Contains(keyWord)))) || (i.NickName.Contains(keyWord)))
                    .OrderBy(i => i.ID)
                    .Skip(Index)
                    .Take(count)
                    .Select<User, BaseUser>(i => new BaseUser()
                    {
                        ID = i.ID,
                        Name = i.Name,
                        NickName = i.NickName,
                        Level = i.Level,
                        RegTime = i.RegTime,
                        Gender = i.Gender,
                        Area = i.Area
                    }
                    ).ToList();
            }
            catch
            {
                return ResultState.DB_ERR;
            }
            return ResultState.SUCCESS;
        }
        #endregion

        #region 用户签到状态
        /// <summary>
        /// 用户签到状态
        /// </summary>
        /// <param name="usr">用户认证信息</param>
        /// <param name="fid">家庭ID</param>
        /// <param name="type">签到类型</param>
        /// <param name="getDate">(可选)签到日期, 默认为当前日期</param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: PERM_DENY, NOT_LOGIN, DB_ERR, SUCCESS, SUCCESS_DUP</returns>
        public ResultState SignState(Identity usr, int fid, UserBalanceType type, DateTime? getDate = null)
        {
            ResultState state;
            DateTime date = getDate ?? DateTime.Today;
            try
            {
                //获取并验证该用户和家庭
                state = Session.GetBusiness<FamilyBusiness>().CheckFamilyMember(usr, fid);
                if (state.IsSuccess())
                {
                    //TODO:要不要针对不同签到类型分别判断
                    state = DbContext.Accounts.Where(a => a.FamilyID == fid && a.UserID == usr.Id && a.CheckInTime == date).Any() ? 
                        ResultState.SUCCESS_DUP : ResultState.SUCCESS;
                }
            }
            catch
            {
                state = ResultState.DB_ERR;
            }
            return state;
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
                        if (usrInfo.Name == usrName && usrInfo.Passwd == usrPass)
                        {
                            int uid = usr.Id;

                            //移除家庭
                            PurgeFamilyWithoutCheck(usrInfo.Family);
                            DbContext.FamilyMembers.RemoveRange(usrInfo.FamilyMember);
                            //移除评论
                            PurgeNoteWithoutCheck(usrInfo.Note);
                            DbContext.NoteComments.RemoveRange(usrInfo.NoteComment);

                            DbContext.Accounts.RemoveRange(usrInfo.Account);
                            DbContext.AppConfigs.RemoveRange(usrInfo.AppConfig);
                            DbContext.BloodPressures.RemoveRange(usrInfo.BloodPressure);
                            DbContext.Locations.RemoveRange(usrInfo.Location);
                            DbContext.MemberComments.RemoveRange(usrInfo.MemberFrom);
                            DbContext.MemberComments.RemoveRange(usrInfo.MemberTo);
                            DbContext.OptLogs.RemoveRange(usrInfo.OptLog);
                            DbContext.Pollings.RemoveRange(usrInfo.Polling);
                            DbContext.Photos.RemoveRange(usrInfo.PhotoFrom);
                            DbContext.Photos.RemoveRange(usrInfo.PhotoTo);
                            DbContext.Shares.RemoveRange(usrInfo.ShareFrom);
                            DbContext.Shares.RemoveRange(usrInfo.ShareTo);
                            DbContext.ThirdShares.RemoveRange(usrInfo.ThirdShare);
                            DbContext.UserMsgs.RemoveRange(usrInfo.MsgFrom);
                            DbContext.UserMsgs.RemoveRange(usrInfo.MsgTo);
                            DbContext.UserTokens.RemoveRange(usrInfo.UserToken);
                            //移除自身
                            DbContext.Users.Remove(usrInfo);
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

        private ResultState PurgeNoteWithoutCheck(ICollection<Note> notes)
        {
            ResultState state = ResultState.SUCCESS;
            try
            {
                foreach (Note n in notes)
                {
                    PurgeNoteCommentsWithoutCheck(DbContext.NoteComments.Where(nc => nc.ToNoteID == n.ID).ToList());
                }
                DbContext.Notes.RemoveRange(notes);
            }
            catch
            {
                state = ResultState.DB_ERR;
            }
            return state;
        }

        private ResultState PurgeNoteCommentsWithoutCheck(ICollection<NoteComment> noteComments)
        {
            ResultState state = ResultState.SUCCESS;
            try
            {
                foreach (NoteComment nc in noteComments)
                {
                    PurgeNoteCommentsWithoutCheck(DbContext.NoteComments.Where(nc2 => nc2.ToNoteCommID == nc.ID).ToList());
                }
                DbContext.NoteComments.RemoveRange(noteComments);
            }
            catch
            {
                state = ResultState.DB_ERR;
            }
            return state;
        }

        private ResultState PurgeFamilyWithoutCheck(ICollection<Family> families)
        {
            ResultState state = ResultState.SUCCESS;
            try
            {
                foreach (Family f in families)
                {
                    DbContext.FamilyMembers.RemoveRange(f.FamilyMember);

                    DbContext.Accounts.RemoveRange(f.Account);

                    DbContext.MemberComments.RemoveRange(f.MemberComment);
                    DbContext.Photos.RemoveRange(f.Photo);
                    DbContext.Shares.RemoveRange(f.Share);
                    DbContext.NoteComments.RemoveRange(f.User_Note_Comment);
                    DbContext.Notes.RemoveRange(f.User_Note);
                }
                DbContext.Families.RemoveRange(families);
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

