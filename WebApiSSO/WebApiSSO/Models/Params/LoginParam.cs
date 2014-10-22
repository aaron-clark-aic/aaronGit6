
namespace WebApiSSO.Api.Models.Params
{
    public class LoginParam
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string Usr { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string Pwd { get; set; }
        /// <summary>
        /// 客户端标识[1: Android, 2: HTML5, 3: 微信, 4: Test]
        /// </summary>
        public byte ClientId { get; set; }
    }
}