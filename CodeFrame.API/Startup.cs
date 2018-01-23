﻿using System.Text;
using CodeFrame.Common;
using CodeFrame.Common.Config;
using CodeFrame.Models;
using CodeFrame.Service.Service;
using CodeFrame.Service.ServiceInterface;
using CodeFrame.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace CodeFrame.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ILogService<Startup> log = new LogService<Startup>();
            log.Info("ConfigureServices开始");

            //DbContext 连接池 2.0版本

            // services.AddDbContextPool<CodeFrameContext>(options => options.UseInMemoryDatabase("mytempdb"));

            services.AddDbContextPool<CodeFrameContext>(options => options.UseMySql(AppConfig.MySqlConnection));



            services.AddUnitOfWork<CodeFrameContext>();//添加UnitOfWork支持

            foreach (var item in ProjectCom.GetClassName("CodeFrame.Service")) //集中注入服务
            {
                foreach (var typeArray in item.Value)
                {
                    services.AddScoped(typeArray, item.Key);
                }
            }
            services.AddScoped(typeof(ILogService<>), typeof(LogService<>));//注入泛型loger
            //添加jwt授权
            services.AddAuthentication(option =>
                {
                    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }) //传入默认授权方案
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidAudience = JwtConfig.JwtConfigModel.Audience,
                        ValidIssuer = JwtConfig.JwtConfigModel.Issuer,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(JwtConfig.JwtConfigModel.SecretKey))
                    };

                });

            //services.AddAutoMapper();//配置autoapper

            services.AddCors(options =>//配置跨域处理
            {
                options.AddPolicy("any", builder =>
                {
                    builder.AllowAnyOrigin() //允许任何来源的主机访问
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();//指定处理cookie
                });
            });
            services.AddMvc()//全局配置Json序列化处理
                .AddJsonOptions(options =>
                    {   
                        //忽略循环引用
                        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                        //不使用驼峰样式的key
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                        //设置时间格式
                        options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                    }
                );
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}