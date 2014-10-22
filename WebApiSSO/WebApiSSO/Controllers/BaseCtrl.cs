using WebApiSSO.Api.Models.Utils;
using System;
using System.Diagnostics;
using System.Web.Http;
using WebApiSSO.BLL;
using WebApiSSO.BLL.Token;

namespace WebApiSSO.Api.Controllers
{
    //NOTE: 不以Controller结尾/abstract -> 防止被框架当成一个实际的控制器
    /// <summary>
    /// ApiController抽象基类, 提供一些工具方法
    /// </summary>
    public abstract class BaseDbCtrl : ApiController
    {
        private DbSession dbSession;
       
        /// <summary>
        /// 获取当前数据库会话
        /// </summary>
        protected DbSession DbSession
        {
            get
            {
                if (dbSession == null)
                {
                    lock (this)
                    {
                        if (dbSession == null)
                        {
                            dbSession = DbSession.Session;
                            Debug(dbSession);
                        }
                    }
                }
                return dbSession;
            }
        }

        [Conditional("DEBUG")]
        private void Debug(DbSession dbSession)
        {
            dbSession.ForLog();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (dbSession != null)
                {
                    dbSession.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 获取用户身份信息(一般情况下使用GetUserIdentity即可)
        /// </summary>
        /// <returns>用户身份信息</returns>
        protected UserInfo GetUserInfo()
        {
            return TokenHelper.GetUserInfo(this);
        }

        /// <summary>
        /// 获取用户认证信息
        /// </summary>
        /// <returns>用户认证信息</returns>
        protected Identity GetUserIdentity()
        {
            return TokenHelper.GetUserIdentity(this);
        }

        /// <summary>
        /// 获取用户ID
        /// </summary>
        /// <param name="uid">(输出)用户ID</param>
        /// <returns>是否登录</returns>
        [Obsolete("因为容易忘记做判断, 弃用此方法. 可考虑直接使用GetUserIdentity()")]
        protected bool TryGetUID(out int uid)
        {
            bool success;
            var identity = GetUserIdentity();
            if (identity != null)
            {
                uid = identity.Id;
                success = true;
            }
            else
            {
                uid = 0;
                success = false;
            }
            return success;
        }

        private static readonly Object locker = new Object();
        private static IUploadHelper uploader = null;
        /// <summary>
        /// 获取文件上传服务
        /// </summary>
        protected IUploadHelper UploadHelper
        {
            get
            {
                if (uploader == null)
                {
                    lock (locker)//typeof加锁会跨进程
                    {
                        if (uploader == null)
                        {
                            uploader = new FormUploadHelper();
                        }
                    }
                }
                return uploader;
            }
        }
    }
}