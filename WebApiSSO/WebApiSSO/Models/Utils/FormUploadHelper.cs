using WebApiSSO.BLL;
using WebApiSSO.BLL.Token;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace WebApiSSO.Api.Models.Utils
{
    /// <summary>
    /// 通过表单上传文件
    /// </summary>
    public class FormUploadHelper : IUploadHelper
    {
        public async Task<Tuple<ResultState, MultipartFormDataStreamProviderWithExt>> GetProviderAsync(HttpRequestMessage request, Identity usr, string basePath, Func<HttpContentHeaders, Identity, string> nameFunc = null)
        {
            ResultState state;
            MultipartFormDataStreamProviderWithExt provider = null;
            try
            {
                provider = await request.Content.ReadAsMultipartAsync(new MultipartFormDataStreamProviderWithExt(usr, basePath, nameFunc));
                state = ResultState.SUCCESS;
            }
            catch (Exception e)
            {
                ArgumentNullException ane = e as ArgumentNullException;
                if (ane != null && "usr" == ane.ParamName)
                {
                    state = ResultState.NOT_LOGIN;
                }
                else
                {
                    state = ResultState.IO_ERR;
                }
            }
            return Tuple.Create(state, provider);
        }
        public Tuple<ResultState, string> TryUploadOne(MultipartFormDataStreamProviderWithExt provider)
        {
            ResultState state = ResultState.SUCCESS;
            string path = null;
            try
            {
                //不从上一级读取, 避免丢失异常
                var file = provider.FileData.FirstOrDefault();
                if (file != null)
                {
                    path = Path.GetFileName(file.LocalFileName);
                }
                else
                {
                    state = ResultState.INV_ARGS;
                }
            }
            catch
            {
                //清理临时文件
                if (path != null)
                    TryDeleteOne(provider.RootPath, path);
                path = null;

                state = ResultState.UNKNOWN_ERR;
            }
            return Tuple.Create(state, path);
        }

        public Tuple<ResultState, IEnumerable<string>> TryUploadMulti(MultipartFormDataStreamProviderWithExt provider)
        {
            ResultState state = ResultState.SUCCESS;
            IList<string> paths = new List<string>();
            try
            {
                if (provider.FileData.Count > 0)
                {
                    foreach (var file in provider.FileData)
                    {
                        //应该不为null, 暂不做处理
                        paths.Add(Path.GetFileName(file.LocalFileName));
                    }
                }
                else
                {
                    state = ResultState.INV_ARGS;
                }
            }
            catch
            {
                //清理临时文件
                TryDeleteMulti(provider.RootPath, paths);
                paths.Clear();

                state = ResultState.UNKNOWN_ERR;
            }
            return Tuple.Create(state, (IEnumerable<string>)paths);
        }

        public ResultState TryDeleteOne(string basePath, string path)
        {
            return TryDeleteMulti(basePath, new string[] { path });
        }

        public ResultState TryDeleteMulti(string basePath, IEnumerable<string> paths)
        {
            ResultState state = ResultState.SUCCESS;
            foreach (var path in paths)
            {
                if (path != null)
                {
                    try
                    {
                        System.IO.File.Delete(basePath + path);
                    }
                    catch
                    {
                        state = ResultState.IO_ERR;
                    }
                }
            }
            return state;
        }

        public class MultipartFormDataStreamProviderWithExt : MultipartFormDataStreamProvider
        {
            private readonly Identity usr;
            public new string RootPath { get { return base.RootPath; } }

            private readonly Func<HttpContentHeaders, Identity, string> nameFunc;
            public MultipartFormDataStreamProviderWithExt(Identity usr, string rootPath, Func<HttpContentHeaders, Identity, string> nameFunc)
                : base(rootPath)
            {
                if (nameFunc == null)
                {
                    if (usr == null) { throw new ArgumentNullException("usr"); }
                }
                this.usr = usr;
                this.nameFunc = nameFunc;

                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }
            }
            public override string GetLocalFileName(HttpContentHeaders headers)
            {
                if (nameFunc == null)
                {
                    if (headers == null)
                    {
                        throw new ArgumentNullException("headers");
                    }
                    string file = headers.ContentDisposition.FileName;
                    if (file != null)
                    {
                        file = file.Trim('"');
                    }
                    //尝试获取原始文件名
                    string name;
                    try
                    {
                        name = Path.GetFileNameWithoutExtension(file);
                    }
                    catch
                    {
                        name = "";
                    }
                    if (String.IsNullOrEmpty(name))
                    {
                        name = "BodyPart";
                    }
                    //获取文件扩展名
                    string ext;
                    try
                    {
                        ext = Path.GetExtension(file);
                    }
                    catch
                    {
                        ext = "";
                    }
                    return string.Format(CultureInfo.InvariantCulture, "{0}{1}_{2}{3}", new object[]
                    {
                        usr.Id,
                        name,
                        Guid.NewGuid(),
                        ext
                    });
                }
                else
                {
                    return nameFunc(headers, this.usr);
                }
            }
        }


        public ResultState TryDeleteOne(FormUploadHelper.MultipartFormDataStreamProviderWithExt provider)
        {
            var file = provider.FileData.FirstOrDefault();
            var path = file != null ? file.LocalFileName : null;
            return TryDeleteOne(provider.RootPath, path);
        }

        public ResultState TryDeleteMulti(FormUploadHelper.MultipartFormDataStreamProviderWithExt provider)
        {
            var paths = provider.FileData.Select(fd => fd.LocalFileName);
            return TryDeleteMulti(provider.RootPath, paths);
        }
    }

    public static class TaskHelper
    {
        /// <summary>
        /// 同步Continue, 不进行线程切换
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Tout"></typeparam>
        /// <param name="task"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task<Tout> ContinueWithSync<T, Tout>(this Task<T> task, Func<Task<T>, Tout> func)
        {
            return task.ContinueWith(func, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
        public static Task ContinueWithSync<T>(this Task<T> task, Action<Task<T>> func)
        {
            return task.ContinueWith(func, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}