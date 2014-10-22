using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace WebApiSSO.BLL.Token
{
    public class UserInfo : IPrincipal
    {
        public string[] Roles { get; internal set; }

        internal UserInfo() { }//提供给EF使用

        public UserInfo(string token, string usrName, int uid, string authType)
            : this(token, usrName, uid, authType, null)
        { }
        public UserInfo(string token, string usrName, int uid, string authType, string[] roles)
        {
            Identity = new Identity(token, usrName, uid, authType);
            //权限控制
            this.Roles = roles;
        }

        public Identity Identity
        {
            get;
            internal set;
        }

        IIdentity IPrincipal.Identity
        {
            get
            {
                return this.Identity;
            }
        }

        public bool IsInRole(string role)
        {
            return Roles == null || Roles.Contains(role);
        }

        bool IPrincipal.IsInRole(string role)
        {
            throw new NotImplementedException();
        }
    }
    public class Identity : IIdentity
    {
        internal Identity() { }//提供给EF使用
        internal Identity(string token, string usrName, int uid, string authType)
        {
            this.Name = usrName;
            this.AuthenticationType = authType;
        }

        public string AuthenticationType { get; internal set; }

        public bool IsAuthenticated
        {
            get { return true; }
        }

        public bool Enable { get; internal set; }

        public string Name { get; internal set; }
        //TODO: 改为internal, 彻底消除不当调用的可能性
        public int Id { get; internal set; }
        public string Token { get; internal set; }
        //TODO: 直接用getter/setter而非SQL实现isNullOrEmpty
        public string NickName { get; internal set; }
    }
}