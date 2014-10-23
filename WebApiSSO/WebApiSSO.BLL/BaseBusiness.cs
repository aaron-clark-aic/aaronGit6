using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using WebApiSSO.DAL;
using Newtonsoft.Json;
using EntityFramework.Extensions;
using WebApiSSO.BLL.Token;
using WebApiSSO.BLL.Utils;

namespace WebApiSSO.BLL
{
    /// <summary>
    /// 业务基类, 实现从Session获取DbContext功能, 及实现使用内部DbSession时的IDisposable
    /// </summary>
    public abstract class BaseBusiness : IDisposable
    {
        private readonly bool autoClose;
        /// <summary>
        /// 获取业务对应的DbSession
        /// </summary>
        /// <returns>返回业务对应的DbSession</returns>
        protected DbSession Session { get; private set; }
        /// <summary>
        /// 从DbSession获取业务对应的数据库上下文
        /// </summary>
        /// <returns>获取业务对应的数据库上下文</returns>
        protected Test9527Entities DbContext { get { return Session.DbContext; } }

        /// <summary>
        /// 使用内部DbSession创建一个业务, 在释放此业务时DbSession将被释放
        /// </summary>
        public BaseBusiness() : this(DbSession.Session, true) { }

        /// <summary>
        /// 使用外部DbSession创建一个业务, 在释放此业务时DbSession将不被释放
        /// </summary>
        public BaseBusiness(DbSession session) : this(session, false) { }

        /// <summary>
        /// 使用外部DbSession创建一个业务, 手动指定是否在在释放此业务时释放DbSession
        /// </summary>
        public BaseBusiness(DbSession session, bool autoClose)
        {
            this.Session = session;
            this.autoClose = autoClose;
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
                if (autoClose)
                {
                    Session.Dispose();
                }
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

        #region 工具函数(验证)
        /// <summary>
        /// 验证用户是否登录
        /// </summary>
        /// <param name="usr">用户认证信息</param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: NOT_LOGIN, SUCCESS</returns>
        protected ResultState CheckLogin(Identity usr)
        {
            return (usr != null) ? ResultState.SUCCESS : ResultState.NOT_LOGIN;
        }
        /// <summary>
        /// 根据用户名获取用户ID
        /// </summary>
        /// <param name="usr">用户认证信息</param>
        /// <param name="uName">用户名(为空表示当前用户)</param>
        /// <param name="uid">(输出)用户ID</param>
        /// <returns>返回状态, 具体含义参见ResultState文档. 可能的值: PERM_DENY, NOT_LOGIN, DB_ERR, SUCCESS</returns>
        [Obsolete("所有会用到这个的接口说明成功的被手机那边忽悠了")]
        protected ResultState GetUidByName(Identity usr, string uName, out int uid)
        {
            uid = 0;
            ResultState state = CheckLogin(usr);
            if (state.IsSuccess())
            {
                //空白查自己
                if (!String.IsNullOrEmpty(uName))
                {
                    try
                    {
                        var info = DbContext.User.Where(u => u.User_Name == uName).Select(u => new { u.User_ID }).FirstOrDefault();
                        if (info != null)
                        {
                            uid = info.User_ID;
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
                else
                {
                    uid = usr.Id;
                    state = ResultState.SUCCESS;
                }
            }
            return state;
        }
        #endregion
    }

    /// <summary>
    /// 为单表查询的业务提供一个简化的实现
    /// (未考虑线程安全)
    /// </summary>
    /// <typeparam name="TEntity">数据实体类</typeparam>
    public class BaseBusiness<TEntity> : BaseBusiness
        where TEntity : class
    {
        /// <summary>
        /// 获取对应的DbSet
        /// </summary>
        /// <returns></returns>
        protected DbSet<TEntity> DbSet { get { return DbContext.Set<TEntity>(); } }

        /// <summary>
        /// 使用内部DbSession创建一个业务, 在释放此业务时DbSession将被释放
        /// </summary>
        public BaseBusiness() : base() { }

        /// <summary>
        /// 使用外部DbSession创建一个业务, 在释放此业务时DbSession将不被释放
        /// </summary>
        public BaseBusiness(DbSession session) : base(session) { }

        /// <summary>
        /// 使用外部DbSession创建一个业务, 手动指定是否在在释放此业务时释放DbSession
        /// </summary>
        public BaseBusiness(DbSession session, bool autoClose) : base(session, autoClose) { }

        #region 增
        /// <summary>
        /// 向待提交列表中标记插入一条记录, 在SaveChanges或DbSession释放时修改才会被实际写入数据库
        /// </summary>
        /// <param name="entity">数据实体</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Add(TEntity entity)
        {
            Session.ForWrite();
            DbSet.Add(entity);
            return true;
        }

        /// <summary>
        /// 向待提交列表中标记插入多条记录, 在SaveChanges或DbSession释放时修改才会被实际写入数据库
        /// </summary>
        /// <param name="entities">数据实体</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Add(IEnumerable<TEntity> entities)
        {
            Session.ForWrite();
            DbSet.AddRange(entities);
            return true;
        }
        #endregion

        #region 删
        /// <summary>
        /// 向待提交列表中标记删除一条记录, 在SaveChanges或DbSession释放时修改才会被实际写入数据库
        /// </summary>
        /// <param name="entity">数据实体</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Remove(TEntity entity)
        {
            Session.ForWrite();
            DbSet.Remove(entity);
            return true;
        }

        /// <summary>
        /// 向待提交列表中标记删除多条记录, 在SaveChanges或DbSession释放时修改才会被实际写入数据库
        /// </summary>
        /// <param name="entities">数据实体</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Remove(IEnumerable<TEntity> entities)
        {
            Session.ForWrite();
            DbSet.RemoveRange(entities);
            return true;
        }
        /// <summary>
        /// 向待提交列表中标记删除符合条件的记录, 在SaveChanges或DbSession释放时修改才会被实际写入数据库
        /// </summary>
        /// <param name="predicate">判断条件</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Remove(Expression<Func<TEntity, bool>> predicate)
        {
            return Remove(Get(predicate));
        }
        /// <summary>
        /// 立即删除符合条件的记录, 相当于Remove + SaveChanges
        /// </summary>
        /// <param name="predicate">判断条件</param>
        /// <returns>返回删除的行数, 无数据修改返回0, 出错返回-1</returns>
        public int RemoveImmed(Expression<Func<TEntity, bool>> predicate)
        {
            int res;
            try
            {
                res = DbSet.Where(predicate).Delete();
            }
            catch
            {
                res = -1;
            }
            return res;
        }

        #endregion

        #region 查
        /// <summary>
        /// 返回对数据表的一个查询
        /// </summary>
        /// <returns>对数据表的一个查询</returns>
        public IQueryable<TEntity> AsQuery()
        {
            return DbSet;
        }
        //据老赵说ToList效率较ToArray高, 未测试(http://www.cnblogs.com/JeffreyZhao/archive/2010/01/21/sort-array-linq-1-notes-and-benchmark.html)
        /// <summary>
        /// 从数据表中查询所有项
        /// </summary>
        /// <returns>查询到的有序数据集</returns>
        public IList<TEntity> Get()
        {
            return Get(null, -1);
        }

        /// <summary>
        /// 从数据表中查询最前面指定条数据
        /// </summary>
        /// <param name="count">返回数据条数, 小于等于0时不限制条数, 大于总条数时返回总条数</param>
        /// <returns>查询到的有序数据集</returns>
        public IList<TEntity> Get(int count)
        {
            return Get(null, count);
        }

        /// <summary>
        /// 从数据表中查询所有满足条件的数据
        /// </summary>
        /// <param name="predicate">判断条件</param>
        /// <returns>查询到的有序数据集</returns>
        public IList<TEntity> Get(Expression<Func<TEntity, bool>> predicate)
        {
            return Get(predicate, -1);
        }

        /// <summary>
        /// 从数据表中查询最前面指定条满足条件的数据
        /// </summary>
        /// <param name="predicate">判断条件</param>
        /// <param name="count">返回数据条数, 小于等于0时不限制条数, 大于总条数时返回总条数</param>
        /// <returns>查询到的有序数据集</returns>
        public IList<TEntity> Get(Expression<Func<TEntity, bool>> predicate, int count)
        {
            IQueryable<TEntity> iq = AsQuery();
            if (predicate != null)
            {
                iq = iq.Where(predicate);
            }
            if (count > 0)
            {
                iq = iq.Take(count);
            }
            return iq.ToList();
        }

        /// <summary>
        /// 从数据表中查询第一条数据, 为空时返回null
        /// </summary>
        /// <returns>查询到的数据实体</returns>
        public TEntity GetFirstOrNull()
        {
            return GetFirstOrNull(null);
        }
        /// <summary>
        /// 从数据表中查询符合条件的第一个元素, 找不到时返回null
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns>查询到的数据实体</returns>
        public TEntity GetFirstOrNull(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate != null)
            {
                return AsQuery().FirstOrDefault(predicate);
            }
            else
            {
                return AsQuery().FirstOrDefault();
            }
        }
        #endregion

        #region 改
        /// <summary>
        /// 使用json字符串更新数据表中指定主键的项
        /// </summary>
        /// <param name="jsonStr">待更新条目的json字符串</param>
        /// <param name="ids">数据表主键</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Update(string jsonStr, params object[] ids)
        {
            Session.ForWrite();
            TEntity oldEntity = DbSet.Find(ids);
            Merge(oldEntity, jsonStr);
            return true;
        }
        /// <summary>
        /// 使用新的完整数据更新数据表中指定主键的项
        /// </summary>
        /// <param name="entity">具有新属性的数据实体</param>
        /// <param name="ids">数据表主键</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Update(TEntity entity, params object[] ids)
        {
            return Update(JsonConvert.SerializeObject(entity), ids);
        }
        /// <summary>
        /// 使用修改函数更新数据表中指定主键的项
        /// </summary>
        /// <param name="modifyFunc">修改函数</param>
        /// <param name="ids">数据表主键</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Update(Action<TEntity> modifyFunc, params object[] ids)
        {
            Session.ForWrite();
            TEntity oldEntity = DbSet.Find(ids);
            modifyFunc(oldEntity);
            return true;
        }
        /// <summary>
        /// 使用json字符串更新数据表中符合指定条件的项
        /// </summary>
        /// <param name="jsonStr">待更新条目的json字符串</param>
        /// <param name="predicate">判断条件</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Update(string jsonStr, Expression<Func<TEntity, bool>> predicate)
        {
            Session.ForWrite();
            foreach (TEntity entity in Get(predicate))
            {
                Merge(entity, jsonStr);
            }
            return true;
        }
        /// <summary>
        /// 使用新的数据更新数据表中符合指定条件的项
        /// </summary>
        /// <param name="entity">具有新属性的数据实体(值为默认值的属性不会被合并)</param>
        /// <param name="predicate">判断条件</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Update(TEntity entity, Expression<Func<TEntity, bool>> predicate)
        {
            return Update(JsonConvert.SerializeObject(entity, jsonPref), predicate);
        }
        /// <summary>
        /// 使用修改函数更新数据表中符合指定条件的项
        /// </summary>
        /// <param name="modifyFunc">修改函数</param>
        /// <param name="predicate">判断条件</param>
        /// <returns>总是返回true, 失败会抛出异常</returns>
        public bool Update(Action<TEntity> modifyFunc, Expression<Func<TEntity, bool>> predicate)
        {
            Session.ForWrite();
            foreach (TEntity entity in Get(predicate))
            {
                modifyFunc(entity);
            }
            return true;
        }
        #endregion

        /// <summary>
        /// 保存之前提交的修改, 若使用外部DbSession初始化时建议使用外部DbSession的相应方法
        /// </summary>
        /// <returns>返回修改的行数, 无数据修改返回0, 出错返回-1</returns>
        public int SaveChanges()
        {
            return Session.SaveChanges();
        }

        private JsonSerializerSettings jsonPref = new JsonSerializerSettings()
        {
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        };
        /// <summary>
        /// 将json字符串中的属性合并到指定对象
        /// </summary>
        /// <param name="dest">需要合并到的对象</param>
        /// <param name="jsonStr">数据实体序列化的json字符串</param>
        protected void Merge(TEntity dest, string jsonStr)
        {
            JsonConvert.PopulateObject(jsonStr, dest);
        }
    }
}
