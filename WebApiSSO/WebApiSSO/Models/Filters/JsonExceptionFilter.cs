using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using WebApiSSO.Api.Models;

namespace WebApiSSO.Api.Filters
{
    public class JsonExceptionFilter : FilterAttribute, IExceptionFilter//, IActionFilter
    {

         //<summary>
         //返回一个合理的ContentType
         //</summary>
         //<returns></returns>
        private string GetJsonContentType(HttpActionExecutedContext context)
        {
            string res = "application/json";
            //var accept = context.Request.Headers.Accept;
            //if (accept != null && !accept.Any((h)=>h.MediaType == "application/json" || h.MediaType == "text/json"))
            //{
            //    res = "text/plain";
            //}
            return res;
        }

        [Conditional("TRACE")]
        private void LogException(HttpActionExecutedContext actionExecutedContext)
        {
            Trace.TraceError(actionExecutedContext.ActionContext.ActionDescriptor.ActionName + ";" +
                JsonConvert.SerializeObject(actionExecutedContext.ActionContext.ActionArguments) + ";" +
                actionExecutedContext.Exception.ToString());
        }

        public Task ExecuteExceptionFilterAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew((obj) =>
            {
                CancellationToken ct = (CancellationToken)obj;
                if (actionExecutedContext.Exception != null)
                {
                    LogException(actionExecutedContext);
                    var res = new ResultModel<Exception>(false, null);
                    var resText = JsonConvert.SerializeObject(res);
                    if (actionExecutedContext.Response == null)
                    {
                        actionExecutedContext.Response = new HttpResponseMessage();
                    }
                    actionExecutedContext.Response.Content = new StringContent(resText, Encoding.UTF8, GetJsonContentType(actionExecutedContext));
                }
            }, cancellationToken, cancellationToken);
        }
    }
}