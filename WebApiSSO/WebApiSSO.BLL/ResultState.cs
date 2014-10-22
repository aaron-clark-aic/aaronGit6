using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebApiSSO.BLL
{
    /// <summary>
    /// 用作返回的状态码
    /// </summary>
    public enum ResultState
    {
        UNKNOWN_ERR = -0xff,

        /// <summary>
        /// 在黑名单中
        /// </summary>
        ERR_IN_BLACK = -20,
        /// <summary>
        /// 找不到家庭
        /// </summary>
        NO_Family = -19,
        /// <summary>
        /// 发送验证码失败
        /// </summary>
        SEND_VFR_ERRO = -18,
        /// <summary>
        /// 用户名已存在
        /// </summary>
        HAS_USER = -17,
        /// <summary>
        /// 数据过期
        /// </summary>
        DATA_OVERDUE= -16,
        /// <summary>
        /// 账号异地登录
        /// </summary>
        REMOTE_LOGIN = -15,
        /// <summary>
        /// IO错误
        /// </summary>
        IO_ERR = -0x14,
        /// <summary>
        /// 未找到指定对象(视使用环境而定)
        /// </summary>
        ERR_NOT_FOUND = -12,
        /// <summary>
        /// 远程服务器未找到
        /// </summary>
        ERR_REMOTE_SERV_NOT_FOUND = -11,
        /// <summary>
        /// 远程服务器连接失败
        /// </summary>
        ERR_REMOTE_SERV_CONN_FAIL = -10,
        /// <summary>
        /// 从远程服务器读取数据出错
        /// </summary>
        ERR_REMOTE_SERV_ERR= -9,
        /// <summary>
        /// 验证码错误
        /// </summary>
        VFY_ERR = -8,
        /// <summary>
        /// 没有权限, 企图获取或设置不属于当前用户的对象信息时会提示此错误
        /// </summary>
        PERM_DENY = -7,
        /// <summary>
        /// 数据冲突
        /// </summary>
        DATA_CONFLICT = -6,
        /// <summary>
        /// 非法参数
        /// </summary>
        INV_ARGS = -5,
        /// <summary>
        /// 用户名或密码错误
        /// </summary>
        PASS_ERR = -4,
        /// <summary>
        /// 找不到此用户
        /// </summary>
        NO_USER = -3,
        /// <summary>
        /// 未登录或登录信息过期
        /// </summary>
        NOT_LOGIN = -2,
        /// <summary>
        /// 数据库出错
        /// </summary>
        DB_ERR = -1,
        /// <summary>
        /// 默认情况, 无异常, 也无特殊描述
        /// </summary>
        SUCCESS = 0,
        /// <summary>
        /// 执行成功, 但只返回部分结果(分页)
        /// </summary>
        SUCCESS_PARTLY = 1,
        /// <summary>
        /// 申请被拒绝
        /// </summary>
        [Obsolete("Using SUCCESS instead.")]
        SUCCESS_DENY = SUCCESS, //2
        /// <summary>
        /// 成功, 但之前已受理过此请求因此未执行或单位时间内执行次数超出限额(重复提交)
        /// </summary>
        SUCCESS_DUP = 3,
    }
}
