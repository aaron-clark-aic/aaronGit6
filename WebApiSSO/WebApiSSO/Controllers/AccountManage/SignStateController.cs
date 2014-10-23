using WebApiSSO.Api.Filters;
using WebApiSSO.Api.Models;
using WebApiSSO.Api.Models.Utils;
using WebApiSSO.BLL.Business;
using System.Web.Http;

namespace WebApiSSO.Api.Controllers
{/// <summary>
    /// 用户签到获取财富API
    /// </summary>
    [CAuthorize]
    public class SignStateController : BaseDbCtrl
    {
        /// <summary>
        /// 获取签到状态
        /// </summary>
        /// <param name="fid">家庭ID</param>
        /// <returns></returns>
        public ResultModel Get([FromUri]int? fid)//在2K3上不加FromUri一直404
        {
            if (fid == null)
            {
                return new ResultModel()
                {
                    State = BLL.ResultState.INV_ARGS
                };
            }
            var res = DbSession.GetBusiness<AccountBusiness>().SignState(this.GetUserIdentity(), (int)fid, UserBalanceType.SignIn);
            return new ResultModel(){
                State = res
            };
        }
    }
}
