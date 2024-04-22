using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Kogel.Cacheing.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "FinanceCenter API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                foreach (var item in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./"), "*.xml"))
                {
                    options.IncludeXmlComments(item, true);
                }
            });

            //Ä¬ÈÏÎªÄÚ´æ»º´æ
            builder.Services.AddCacheing();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "FinanceCenter API");
            });

            app.MapControllers();

            app.Run();
        }
    }
}
