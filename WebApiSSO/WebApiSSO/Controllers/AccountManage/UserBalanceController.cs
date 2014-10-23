using WebApiSSO.Api.Filters;
using WebApiSSO.Api.Models;
using WebApiSSO.Api.Models.Utils;
using WebApiSSO.BLL.Business;

namespace WebApiSSO.Api.Controllers
{
    /// <summary>
    /// 用户签到获取财富API
    /// </summary>
    [CAuthorize]
    public class UserBalanceController : BaseDbCtrl
    {
        /// <summary>
        /// 签到
        /// </summary>
        /// <returns></returns>
        public ResultModel Post(UserBalanceParam param)
        {
            //验证参数
            if (!ParamHelper.CheckAllAttNoEmpty(param, param.GetType(), "Comment"))
            {
                return new ResultModel()
                {
                    State = BLL.ResultState.INV_ARGS
                };
            }
            var res = DbSession.GetBusiness<AccountBusiness>().UserGetBalance(this.GetUserIdentity(), param);
            return new ResultModel()
            {
                State = res
            };
        }
    }
}