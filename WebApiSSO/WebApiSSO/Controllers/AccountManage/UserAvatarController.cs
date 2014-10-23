using WebApiSSO.Api.Filters;
using WebApiSSO.Api.Models;
using WebApiSSO.BLL;
using WebApiSSO.BLL.Business;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebApiSSO.Api.Controllers
{
    /// <summary>
    /// 用户头像设置API
    /// </summary>
    [CAuthorize]
    public class UserAvatarController : BaseDbCtrl
    {
        private static readonly string ROOT_PATH;

        static UserAvatarController()
        {
            string physicDicPath;
            physicDicPath = AppDomain.CurrentDomain.BaseDirectory + ConfigurationManager.AppSettings["appUserAvatarSavePath"];
            //查看物理路径是否存在
            if (!Directory.Exists(physicDicPath))
            {
                Directory.CreateDirectory(physicDicPath);
            }
            ROOT_PATH = physicDicPath;
        }

        /// <summary>
        /// 上传头像图片
        /// </summary>
        /// <returns></returns>
        public async Task<ResultModel<string>> Post()
        {
            ResultModel<string> rm = new ResultModel<string>();
            var id = this.GetUserIdentity().Id;
            var provider = new MultipartFormDataStreamProvider(ROOT_PATH);
            await Request.Content.ReadAsMultipartAsync(provider).ContinueWith(_task =>
            {
                try
                {
                    //用_task.Result而非provider避免造成异常的丢失
                    var file = _task.Result.FileData[0];
                    var extrend = "." + file.Headers.ContentDisposition.FileName.Trim('"').Split('.')[1];//从正文中获取文件扩展名
                    //获取完整文件路径（文件名永远是唯一的）
                    string fullPath = file.LocalFileName + extrend;
                    //旧的文件名
                    string oldFileName;
                    //临时文件名
                    string tempFileName = file.LocalFileName.Substring(file.LocalFileName.LastIndexOf('\\') + 1);
                    byte[] b = System.Text.Encoding.Default.GetBytes(tempFileName);
                    //新的文件名
                    string newFileName = "pic_" + Convert.ToBase64String(b).Substring(0, 8) + "_" + Guid.NewGuid().ToString() + extrend;
                    {
                        //先移动文件
                        //重命名新的
                        File.Move(file.LocalFileName, ROOT_PATH + newFileName);
                        File.Delete(file.LocalFileName);
                    }
                    //尝试将文件名写入表
                    var res = DbSession.ForWrite().GetBusiness<AccountBusiness>().WritePhoneUrl(id, newFileName, out oldFileName);
                    //如果成功
                    if (res == ResultState.SUCCESS)
                    {
                        if (!String.IsNullOrEmpty(oldFileName))
                        {
                            //删除旧的
                            File.Delete(ROOT_PATH + oldFileName);
                        }
                        rm.State = ResultState.SUCCESS;
                        rm.Data = newFileName;
                    }
                    else
                    {
                        File.Delete(file.LocalFileName);
                        rm.State = res;
                    }
                }
                catch
                {
                    rm.State = ResultState.IO_ERR;
                }
            });
            return rm;

            #region 旧方法弃用（由于httpcontext可能为空）
            /* 
             try
             {
                 string CongfigPath = ConfigurationManager.AppSettings["appUserAvatarSavePath"];
                 //虚拟路径名
                 string HostPath = HttpRuntime.AppDomainAppVirtualPath + CongfigPath;               
                 //物理路径名
                 string PhysicPath = HttpRuntime.AppDomainAppPath + CongfigPath;
                 if (HttpContext.Current.Request.Files.Count == 0)
                 {
                     rm.State = ResultState.INV_ARGS;
                     return rm;
                 }
                 var file = HttpContext.Current.Request.Files[0];//获取正文图片
                 if (!Directory.Exists(PhysicPath))
                 {
                     Directory.CreateDirectory(PhysicPath);
                 }
                 string fullPath = PhysicPath + '\\' + username + file.FileName.Substring(file.FileName.IndexOf('.'));
                 file.SaveAs(fullPath);//保存图片,图片应该很小，就不分块了
                 //将图片地址写入数据库
                 var w_r = DbSession.ForWrite().GetBusiness<AccountBusiness>().WritePhoneUrl(username, HostPath + username + file.FileName.Substring(file.FileName.IndexOf('.')));
                 //写入异常就删除文件
                 if (w_r != ResultState.SUCCESS)
                 {
                     File.Delete(fullPath);                  
                 }
                 rm.State = w_r;
                 return rm;
             }
             catch
             {
                 rm.State = ResultState.UNKNOWN_ERR;
                 return rm;
             }
         }*/
            #endregion
        }

        /// <summary>
        /// 下载头像图片
        /// </summary>
        /// <param name="photoUrl">图片地址</param>
        /// <returns></returns>
        public HttpResponseMessage Get(string photoUrl)
        {
            HttpResponseMessage res = new HttpResponseMessage();
            if (string.IsNullOrEmpty(photoUrl))
            {
                res.StatusCode = HttpStatusCode.NotFound;
                return res;
            }
            //图片的实际路径
            string physicDicPath = HttpRuntime.AppDomainAppPath + ConfigurationManager.AppSettings["appUserAvatarSavePath"] + photoUrl;      
            //文件不存在
            if (!File.Exists(physicDicPath))
            {
                //返回404
                res.StatusCode = HttpStatusCode.NotFound;
                return res;
            }
            try
            {

                using (var f_s = File.OpenRead(physicDicPath))
                {
                    //定义图片字节数组
                    byte[] bt = new byte[f_s.Length];
                    int bufferSize = 8096;
                    //缓冲字节
                    byte[] buffer = new byte[bufferSize];
                    //计数
                    long count = 0;
                    while (count < f_s.Length)
                    {
                        if (count + bufferSize <= f_s.Length)
                        {
                            f_s.Read(buffer, 0, bufferSize);
                        }
                        else
                        {
                            buffer = new byte[f_s.Length - count];
                            f_s.Read(buffer, 0, Convert.ToInt32(f_s.Length - count));
                        }
                        //将缓冲字节追加字节数组
                        buffer.CopyTo(bt, count);
                        count += bufferSize;
                    }
                    //写入正文和状态
                    res.Content = new ByteArrayContent(bt);
                    res.StatusCode = HttpStatusCode.OK;
                    //写入MIME类型
                    res.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(@"image/" + photoUrl.Substring(photoUrl.LastIndexOf('.') + 1));
                }
            }
            catch
            {
                res.StatusCode = HttpStatusCode.BadRequest;
            }
            return res;
        }
    }
}
