using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Swagger;
using System.IO;

namespace Kogel.Cacheing.Test.Swagger
{
    public static class SwaggerBuilderExtensions
    {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="app"></param>
		/// <param name="apiName"></param>
		public static void UseSwaggers(this IApplicationBuilder app, string apiName)
		{
			//啟用中間件服務生成Swagger作為JSON終結點
			app.UseSwagger();
			//啟用中間件服務對swagger-ui，指定Swagger JSON終結點
			app.UseSwaggerUI(c =>
			{
				//根據版本名稱倒序 遍歷展示
				typeof(ApiVersions).GetEnumNames().OrderByDescending(e => e).ToList().ForEach(version =>
				{
					c.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{apiName} {version}");
				});
				// 將swagger首頁，設置成我們自定義的頁面，記得這個字符串的寫法：解決方案名.index.html
				// c.IndexStream = () => GetType().GetTypeInfo().Assembly.GetManifestResourceStream("Fresh.AppAPI.index.html");//這里是配合MiniProfiler進行性能監控的。
				c.RoutePrefix = ""; //路徑配置，設置為空，表示直接在根域名（localhost:8001）訪問該文件,注意localhost:8001/swagger是訪問不到的，去launchSettings.json把launchUrl去掉
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="services"></param>
		/// <param name="apiName"></param>
		/// <param name="pathArr"></param>
		public static void AddSwaggerGens(this IServiceCollection services, string apiName, string[] pathArr = null)
		{
			var basePath = AppContext.BaseDirectory;
			//注冊Swagger生成器，定義一個和多個Swagger 文檔
			services.AddSwaggerGen(options =>
			{
				typeof(ApiVersions).GetEnumNames().ToList().ForEach(version =>
				{
					options.SwaggerDoc(version, new Info
					{
						Version = version,
						Title = $"{apiName} 接口文檔",
						Description = $"{apiName} 接口文檔說明 " + version,
					});
					//自定義配置文件路徑
					if (pathArr != null)
					{
						foreach (var item in pathArr)
						{
							var path = Path.Combine(basePath, item);
							options.IncludeXmlComments(path, true);
						}
					}
					// 按相對路徑排序
					options.OrderActionsBy(o => o.RelativePath);
				});
				var xmlPath = Path.Combine(basePath, $"{apiName}.xml");//xml文件名
				options.IncludeXmlComments(xmlPath, true);//默認的第二個參數是false，這個是controller的注釋，
			});
		}

		/// <summary>
		/// Api接口版本 自定義
		/// </summary>
		public enum ApiVersions
		{
			/// <summary>
			/// V1 版本
			/// </summary>
			V1 = 1,
		}
	}
}
