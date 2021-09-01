using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DotNetCoreReverseProxy.Middlewares
{
	public class RestrictorMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private IMemoryCache _cache;

        // 限流設定
        private static List<RestrictorSetting> restrictorSettings = new List<RestrictorSetting>()
        {
            new RestrictorSetting(){ Target = "/" },
            new RestrictorSetting(){ Target = "/Home/Privacy" },
            new RestrictorSetting(){ Target = "/googleforms/d/e/1FAIpQLSdJwmxHIl_OCh-CI1J68G1EVSr9hKaYFLh3dHh8TLnxjxCJWw/viewform" },
        };

        public RestrictorMiddleware(RequestDelegate nextMiddleware, IMemoryCache memoryCache)
        {
            _nextMiddleware = nextMiddleware;
            _cache = memoryCache;
        }

        public async Task Invoke(HttpContext context)
        {
            // 取得網址
            Uri orignalUri = new Uri(UriHelper.GetEncodedUrl(context.Request));

            // 判斷該網址是否有達到限流狀態
            (bool isSuccess, string errorMsg) = Check(orignalUri.AbsolutePath);
            if (!isSuccess) 
            {
                // 達到限流回傳錯誤訊息
                await context.Response.WriteAsync(errorMsg, Encoding.UTF8);
            }

            await _nextMiddleware(context);
        }

        /// <summary>
        /// 檢查是否達限流上限
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
		private (bool isSuccess, string errorMsg) Check(string absolutePath)
		{
            if (!restrictorSettings.Any(x => x.Target == absolutePath)) 
            {
                // 沒有限流設定
                return (true, "");
            }

            // 取得限流設定
            var setting = restrictorSettings.Where(x => x.Target == absolutePath).First();

            // 取得限流紀錄
            var cacheKey = $"Restrictor.{setting.Target}";
            if (!_cache.TryGetValue(cacheKey, out int count))
            {
                // Create a timer with a two second interval.
                var timer = new Timer(setting.LimitSec * 1000);
                // Hook up the Elapsed event for the timer. 
                timer.Elapsed += async (sender, e) => await HandleTimer(_cache, setting) ;
                timer.AutoReset = true;
                timer.Enabled = true;

                return (true, "");
            }
            else 
            {
                // 已達限流 (沒有令牌可取)
                if (count <= 0) 
                {
                    return (false, "已達限流");
                }

                // 未達限流 (取一個令牌)
                _cache.Set(cacheKey, --count);
                
                return (true, "");
            }
        }

        /// <summary>
        /// 設定限流(建立令牌桶)
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
		private async Task HandleTimer(IMemoryCache cache, RestrictorSetting setting)
		{
            var cacheKey = $"Restrictor.{setting.Target}";
            if (!_cache.TryGetValue(cacheKey, out int count)) 
            {
                cache.Set(cacheKey, setting.LimitTimes);
                await Task.CompletedTask;
            }
                
        }
    }

    /// <summary>
    /// 限流設定
    /// </summary>
    class RestrictorSetting 
    {
        /// <summary>
        /// 設定目標
        /// </summary>
		public string Target { get; set; }

        /// <summary>
        /// 時間(秒)
        /// </summary>
		public int LimitSec { get; set; } = 10;

        /// <summary>
        /// 次數(秒)
        /// </summary>
        public int LimitTimes { get; set; } = 3;

        /// <summary>
        /// 是否啟用
        /// </summary>
		public bool Enable { get; set; } = true;
    }

}
