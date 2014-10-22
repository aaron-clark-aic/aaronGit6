using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;

namespace WebApiSSO.BLL.Utils
{
    public static class ResultStateHelper
    {

        public static string FormatState(this ResultState state)
        {
            string msg;
            switch (state)
            {
                case ResultState.SUCCESS_PARTLY:
                    msg = "操作成功(分段)";
                    break;
                case ResultState.SUCCESS:
                    msg = "操作成功";
                    break;
                case ResultState.NO_USER:
                    msg = "用户不存在";
                    break;
                case ResultState.PASS_ERR:
                    msg = "用户名或密码错误";
                    break;
                case ResultState.DB_ERR:
                    msg = "数据库错误";
                    break;
                case ResultState.NOT_LOGIN:
                    msg = "未登录或登录信息超时";
                    break;
                case ResultState.INV_ARGS:
                    msg = "非法参数";
                    break;
                case ResultState.DATA_CONFLICT:
                    msg = "数据冲突";
                    break;
                case ResultState.PERM_DENY:
                    msg = "没有权限";
                    break;
                case ResultState.VFY_ERR:
                    msg = "验证码错误";
                    break;
                case ResultState.REMOTE_LOGIN:
                    msg = "异地登陆";
                    break;
                case ResultState.HAS_USER:
                    msg = "用户已存在";
                    break;
                case ResultState.DATA_OVERDUE:
                    msg = "数据过期";
                    break;
                case ResultState.SEND_VFR_ERRO:
                    msg = "网关发送验证码失败";
                    break;
                case ResultState.UNKNOWN_ERR:
                    msg = "未知错误";
                    break;
                default:
                    msg = Enum.GetName(typeof(ResultState), state);
                    if (String.IsNullOrEmpty(msg))
                    {
                        msg = state.IsSuccess() ? "操作成功(其它)" : "未知错误";
                    }
                    break;
            }
            return msg;
        }

        //NOTE:TargetedPatchingOptOut用于指示这个方法的逻辑长期稳定，可以跨库进行内联
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
        public static bool IsSuccess(this ResultState state)
        {
            return (state >= 0);
        }
    }
}
