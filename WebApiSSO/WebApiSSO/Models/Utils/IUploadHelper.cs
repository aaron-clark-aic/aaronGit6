using WebApiSSO.BLL;
using WebApiSSO.BLL.Token;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace WebApiSSO.Api.Models.Utils
{
    /// <summary>
    /// 线程安全的文件上传接口, 注册给HttpConfiguration.Services
    /// </summary>
    public interface IUploadHelper
    {
        //FIXME:返回和传入MultipartFormDataStreamProviderWithExt的实现有问题, 但暂时没有解决方式
        Task<Tuple<ResultState, FormUploadHelper.MultipartFormDataStreamProviderWithExt>> GetProviderAsync(HttpRequestMessage request, Identity usr, string basePath, Func<HttpContentHeaders, Identity, string> nameFunc = null);

        Tuple<ResultState, string> TryUploadOne(FormUploadHelper.MultipartFormDataStreamProviderWithExt provider);

        Tuple<ResultState, IEnumerable<string>> TryUploadMulti(FormUploadHelper.MultipartFormDataStreamProviderWithExt provider);

        ResultState TryDeleteOne(string basePath, string path);

        ResultState TryDeleteOne(FormUploadHelper.MultipartFormDataStreamProviderWithExt provider);

        ResultState TryDeleteMulti(FormUploadHelper.MultipartFormDataStreamProviderWithExt provider);
    }
}
