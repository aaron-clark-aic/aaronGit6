using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using EntityFramework.Extensions;
using WebApiSSO.DAL;
using WebApiSSO.BLL.Utils;
using System.Diagnostics;
using System.IO;
using System.Collections.Concurrent;


namespace WebApiSSO.BLL.Token
{
    public class TokenCacheProvider : ITokenProvider, IDisposable
    {
        private readonly ITokenProvider provider;
        //TODO: LRU cahce
        //NOTE: 非进程安全
        private readonly ConcurrentDictionary<string, UserInfo> uiCache = new ConcurrentDictionary<string, UserInfo>();
        public TokenCacheProvider(ITokenProvider provider)
        {
            this.provider = provider;
        }

        public ResultState TryRenewToken(string usr, string pwd, byte from, string userAgent, ref string token)
        {
            return provider.TryRenewToken(usr, pwd, from, userAgent, ref token);
        }

        public UserInfo GetUser(string token, string authType)
        {
            UserInfo userInfo;
            GetUser(token, authType, out userInfo);
            return userInfo;
        }

        public ResultState GetUser(string token, string authType, out UserInfo userInfo)
        {
            ResultState state;
            try
            {
                userInfo = uiCache.GetOrAdd(token, (tk) =>
                {
                    UserInfo ui;
                    ResultState rs;
                    rs = provider.GetUser(tk, authType, out ui);
                    if (rs.IsSuccess())
                    {
                        return ui;
                    }
                    else
                    {
                        throw new NotCacheStateException<UserInfo>(rs, ui);
                    }
                });
                state = ResultState.SUCCESS;
            }
            catch (NotCacheStateException<UserInfo> e)
            {
                state = e.State;
                userInfo = e.Value;
            }

            return state;
        }

        public ResultState InvalidToken(string token)
        {
            ResultState state = provider.InvalidToken(token);
            if (state.IsSuccess())
            {
                UserInfo ui;
                uiCache.TryRemove(token, out ui);
            }
            return state;
        }
        #region IDisposable Members
        /// <summary>
        /// Internal variable which checks if Dispose has already been called
        /// </summary>
        private Boolean disposed;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                //Managed cleanup code here, while managed refs still valid
                provider.Dispose();
            }
            //Unmanaged cleanup code here

            disposed = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Call the private Dispose(bool) helper and indicate 
            // that we are explicitly disposing
            this.Dispose(true);

            // Tell the garbage collector that the object doesn't require any
            // cleanup when collected since Dispose was called explicitly.
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// 用于检测业务是否已被Dispose
        /// </summary>
        public bool IsDisposed { get { return disposed; } }
        #endregion

        [Serializable]
        private class NotCacheStateException : Exception
        {
            public ResultState State { get; private set; }
            public NotCacheStateException(ResultState state)
            {
                this.State = state;
            }
        }

        private class NotCacheStateException<T> : NotCacheStateException
        {
            public T Value { get; private set; }
            public NotCacheStateException(ResultState state, T data)
                : base(state)
            {
                this.Value = data;
            }
        }


        public ResultState AddUser(string username, string password)
        {
            return provider.AddUser(username, password);
        }

        public ResultState ResetPwd(string username, string newPwd)
        {
            ResultState state = provider.ResetPwd(username, newPwd);
            if (state.IsSuccess())
            {
                UserInfo ui;
                foreach (var kvp in uiCache.Where(kvp => kvp.Value.Identity.Name == username))
                {
                    uiCache.TryRemove(kvp.Key, out ui);
                }
            }
            return state;
        }

        public ResultState ModifyPwd(Identity usr, string oldPwd, string newPwd)
        {
            ResultState state = provider.ModifyPwd(usr, oldPwd, newPwd);
            if (state.IsSuccess())
            {
                UserInfo ui;
                foreach (var kvp in uiCache.Where(kvp => kvp.Value.Identity.Id == usr.Id))
                {
                    uiCache.TryRemove(kvp.Key, out ui);
                }
            }
            return state;
        }

        #region protected
        public ResultState VfyPwd(string usr, string pwd, string token, out int uid)
        {
            return provider.VfyPwd(usr, pwd, token, out uid);
        }

        public ResultState InvalidAllToken(int uid)
        {
            ResultState state = provider.InvalidAllToken(uid);
            if (state.IsSuccess())
            {
                UserInfo ui;
                foreach (var kvp in uiCache.Where(kvp => kvp.Value.Identity.Id == uid))
                {
                    uiCache.TryRemove(kvp.Key, out ui);
                }
            }
            return state;
        }

        public ResultState InvalidAllOtherToken(Identity usr)
        {
            ResultState state = provider.InvalidAllOtherToken(usr);
            if (state.IsSuccess())
            {
                UserInfo ui;
                foreach (var kvp in uiCache.Where(kvp => kvp.Value.Identity.Id == usr.Id))
                {
                    uiCache.TryRemove(kvp.Key, out ui);
                }
            }
            return state;
        }
        #endregion
    }
}