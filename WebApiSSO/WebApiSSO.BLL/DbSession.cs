using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Diagnostics;
using WebApiSSO.BLL.Token;
using WebApiSSO.DAL;

namespace WebApiSSO.BLL
{
    /// <summary>
    /// 一个自定义的用于延迟按需初始化的工具类, 和微软Lazy&lt;T&gt;的区别是增加了对dispose的支持, 移除了对异常的支持, 简化了对工厂方法的支持
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MyLazy<T> : IDisposable where T : new()
    {
        //无参构造, see http://www.cnblogs.com/xuanyuge/archive/2011/07/06/2099552.html

        /// <summary>
        /// 标记值是否已被初始化
        /// </summary>
        private bool valueCreated = false;
        /// <summary>
        /// 存储已初始化的值
        /// </summary>
        private T value;

        public MyLazy() { }

        /// <summary>
        /// 获取值是否已被初始化
        /// </summary>
        /// <returns></returns>
        public bool IsValueCreated
        {
            get
            {
                return valueCreated;
            }
        }
        /// <summary>
        /// 若未初始化值, 则初始化并返回, 否则直接返回已初始化的值; 若初始化过程遇到异常则直接抛出, 并在下次再次尝试初始化
        /// </summary>
        /// <exception cref="ObjectDisposedException">MyLazy&lt;T&gt;已释放</exception>
        public T Value
        {
            get
            {
                if (!valueCreated)
                {
                    lock (this)
                    {
                        if (!valueCreated)
                        {
                            if (disposed)
                                throw new ObjectDisposedException("this", "MyLazy is disposed.");

                            value = new T();
                            valueCreated = true;
                        }
                    }
                }
                return value;
            }
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

            //加锁保证在Dispose之后不会执行Value的创建
            lock (this)
            {
                if (disposing)
                {
                    //Managed cleanup code here, while managed refs still valid
                    if (valueCreated)
                    {
                        IDisposable val = value as IDisposable;
                        if (val != null)
                        {
                            val.Dispose();
                        }
                    }
                }
                //Unmanaged cleanup code here

                disposed = true;
            }
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
        #endregion
    }
    //TODO:使用值类型??
    /// <summary>
    /// 获取一个DbSession(对DbContext的封装, 表示一系列事务的数据库操作)
    /// 非线程安全
    /// </summary>
    public class DbSession: IDisposable
    {
        /// <summary>
        /// 标记是否写入数据, 若存在数据写入会在dispose时尝试SaveChanges
        /// </summary>
        private bool forWrite;

        /// <summary>
        /// 数据库上下文(按需初始化)
        /// </summary>
        private readonly MyLazy<Test9527Entities> dbContext = new MyLazy<Test9527Entities>();

        /// <summary>
        /// 获取DbSession对应的数据库上下文
        /// </summary>
        /// <returns>数据库上下文</returns>
        public Test9527Entities DbContext
        {
            get
            {
                return dbContext.Value;
            }
        }

        /// <summary>
        /// 工厂方法, 获取一个新的DbSession
        /// </summary>
        /// <returns></returns>
        public static DbSession Session
        {
            get
            {
                return new DbSession();
            }
        }

        private DbSession() : this(false) { }

        private DbSession(bool write)
        {
            this.forWrite = write;
        }

        public DbSession ForLog()
        {
            DbContext.Database.Log = s=>Trace.Write(s);
            return this;
        }

        #region 业务工厂
        ///// <summary>
        ///// 二级缓存字典(静态), 按业务类型缓存和反射来的使用DbSession生成业务的工厂方法
        ///// </summary>
        private static readonly ConcurrentDictionary<Type, Func<DbSession, BaseBusiness>> businessFactMap = new ConcurrentDictionary<Type, Func<DbSession, BaseBusiness>>();
        ///// <summary>
        ///// 一级缓存字典(按需初始化), 按业务类型缓存和当前DbSession相关的业务实例
        ///// </summary>
        private readonly MyLazy<ConcurrentDictionary<Type, BaseBusiness>> businessMap = new MyLazy<ConcurrentDictionary<Type, BaseBusiness>>();
        private ConcurrentDictionary<Type, BaseBusiness> BusinessMap
        {
            get
            {
                return businessMap.Value;
            }
        }

        private ITokenProvider tokenProvider;
        public ITokenProvider GetITokenProvider()
        {
            if (tokenProvider == null || tokenProvider.IsDisposed)
            {
                lock (this)
                {
                    if (tokenProvider == null || tokenProvider.IsDisposed)
                    {
                        tokenProvider = new TokenProvider(this);
                    }
                }
            }
            return tokenProvider;
        }


        /// <summary>
        /// [测试优化函数个数]获取和DbSession相关的业务实例
        /// </summary>
        /// <typeparam name="T">业务类型</typeparam>
        /// <returns>和当前DbSession相关的业务实例</returns>
        /// <exception cref="InvalidOperationException">指定的类型不具有使用DbSession的构造函数</exception>
        public T GetBusiness<T>() where T : BaseBusiness//, new()
        {
            if (typeof(T) != typeof(TokenProvider))
            {
                //在一级缓存中查找指定业务类型的实例, 否则用二级缓存中的工厂方法生成
                Func<Type, BaseBusiness> valueFact = (type) =>
                {
                    //在二级缓存中查找指定业务类型的工厂方法, 否则用反射生成
                    return businessFactMap.GetOrAdd(type, (type2) =>
                    {
                        //获取指定类型的具有一个参数DbSession的构造函数
                        var init = type2.GetConstructor(new Type[] { typeof(DbSession) });
                        //找不到, 报错
                        if (init == null)
                            throw new InvalidOperationException("指定的类型不具有使用DbSession的构造函数");
#if true
                        //NOTE:从微软抄的加自己整理, 有点不明觉厉
                        //使用表达式树将其编译为函数
                        var pars = new ParameterExpression[] { Expression.Parameter(typeof(DbSession), "session") };
                        NewExpression body = Expression.New(init, pars);
                        return Expression.Lambda<Func<DbSession, BaseBusiness>>(body, pars).Compile();
#else
                        return (session) => init.Invoke(new object[] { session });
#endif
                    })(this);
                };
                Type key = typeof(T);
                BaseBusiness res = BusinessMap.GetOrAdd(key, valueFact);
                if (res.IsDisposed)
                {
                    //如果之前分配的业务已被释放, 分配一个新的
                    res = valueFact(key);
                    BusinessMap[key] = res;
                }
                return (T)res;
            }
            else
            {
                //对TokenProvider特别处理
                return (T)GetITokenProvider();
            }
        }
        #endregion
        /// <summary>
        /// 正式保存此Session中修改的数据
        /// </summary>
        /// <returns>返回修改的行数, 无数据修改返回0, 出错返回-1</returns>
        public int SaveChanges()
        {
            int res;
            if (dbContext.IsValueCreated)
            {
                try
                {
                    res = DbContext.SaveChanges();
                }
                catch
                {
                    res = -1;
                }
            }
            else
            {
                res = 0;
            }
            return res;
        }

        /// <summary>
        /// 将DbSession设置为准备写入
        /// </summary>
        /// <returns>链式调用, 返回自身</returns>
        public DbSession ForWrite()
        {
            this.forWrite = true;
            return this;
        }

        //TODO: 日志
        /// <summary>
        /// 日志操作的记录
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public DbSession Log4EF(string message, Log4EF_Level level, string tag)
        {
            //TODO: 日志的相关操作
            return ForWrite();
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
                if (forWrite && dbContext.IsValueCreated)
                {
                    //若已初始化数据库上下文且标记为写入, 则在释放之前保存数据库上下文
                    SaveChanges();
                }
                //释放数据库上下文
                //businessMap.Dispose();
                dbContext.Dispose();
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
        /// The destructor for the class.
        /// </summary>
        //~DbSession()
        //{
        //    this.Dispose(false);
        //}


#endregion
        
    }

    /// <summary>
    /// log4EF enum 
    /// </summary>
    public enum Log4EF_Level
    {      
        EMERG = 1,
        ALERT = 2,
        CRIT = 3,
        ERROR = 4,
        WARN = 5,
        NOTICE = 6,
        INFO = 7,
        DEBUG = 8
    }
}