//using assetmanagement.MyDBContext;
using Castle.Core.Configuration;
using EntityProject.DBContext;
using EntityProject.repositories;
using EntityProject.repositoriesimpl;
using EntityProject.Services;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using Quartz;
using Retailbanking.BL.IServices;
using Retailbanking.BL.Services;
using Retailbanking.BL.utils;
using Retailbanking.Common.CustomObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace assetmanagement
{
    public class Startup
    {
       // private readonly IConfiguration Configuration;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IRegistration, RegistrationServices>();
            services.AddSingleton<IUserCacheService, UserCacheService>();
          //  services.AddSingleton<IPinService,PinService>();
            services.AddSingleton<IRedisStorageService, RedisStorageService>();
            services.AddSingleton<IGeneric, GenericServices>();
            services.AddSingleton<IPortfolioService,PortfolioService>();
            services.AddSingleton<IGenericAssetCapitalInsuranceCustomerService,GenericAssetCapitalInsuranceCustomerService>();
            services.AddSingleton<IRedisStorageService, RedisStorageService>();
            services.AddSingleton<IGeneric, GenericServices>();
            services.AddSingleton<ISmsBLService, SmsBLService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IFlutterPaymentLink,FlutterPaymentLink>();
            services.AddSingleton<IRedemptionService,RedemptionService>();
            services.AddSingleton<IAsynEmailSenderWrapper,AsynEmailSenderWrapper>();
            services.AddSingleton<IAssetCapitalInsuranceKycService,AssetCapitalInsuranceKycService>();
            services.AddSingleton<TemplateService>(); // Register your template service
            services.AddSingleton<EntityDapperContext>();
            services.AddSingleton<DapperContext>();
            var appSetting = Configuration.GetSection("AppSettingConfig");
            services.Configure<AppSettings>(appSetting);
            services.AddHttpContextAccessor();
            var assetSimplexConfig = Configuration.GetSection("AssetSimplexConfig");
            services.Configure<AssetSimplexConfig>(assetSimplexConfig);
            //for entity framework
            services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(Configuration.GetConnectionString("dbconn"),
             b => b.MigrationsAssembly("EntityProject")));
            services.AddScoped<IRegistrationRepository, RegistrationRepository>();
            services.AddScoped<IUserRepository,UserRepository>();
            services.AddScoped<IUserSessionRepository, UserSessionRepository>();
            services.AddScoped<IUserCredentialsRepository,UserCredentialsRepository>();
            services.AddScoped<IMobileDeviceRepository,MobileDeviceRepository>();
            services.AddScoped<IOtpSessionRepository,OtpSessionRepository>();
            services.AddScoped<IUserSessionRepository,UserSessionRepository>();
            services.AddScoped<ICustomerDeviceRepository,CustomerDeviceRepository>();
            services.AddScoped<ICustomerDataNotFromBvnRepository,CustomerDataNotFromBvnRepository>();
            services.AddScoped<IRegistrationSessionRepository,RegistrationSessionRepostory>();
            services.AddScoped<IHistoricalPerformance,HistoricalPerformance>();
            services.AddScoped<ITMMF,TMMF>();
            services.AddScoped<IClientBankRepository,ClientBankRepository>();
            services.AddScoped<ISignatoryRepository,SignatoryRepository>();
            services.AddScoped<IUtilityBillRepository,UtilityBillRepository>();
            services.AddScoped<IIdCardRepository,IdCardRepository>();
            services.AddScoped<IPaymentOutsideOfFlutterwave,PaymentOutsideOfFlutterwave>();
            services.AddScoped<IBvnValidationRepository,BvnValidationRepository>();
            services.AddScoped<ISimplexLiquidationServiceRequestRepository,SimplexLiquidationServiceRequestRepository>();
            var folderPaths = Configuration.GetSection("FolderPaths");
            services.Configure<FolderPaths>(folderPaths);
            var otpMsg = Configuration.GetSection("OtpMessages");
            services.Configure<OtpMessage>(otpMsg);
            var smtpDetails = Configuration.GetSection("SMTPDetails");
            services.Configure<SmtpDetails>(smtpDetails);
            // var otpMsg = Configuration.GetSection("OtpMessages");
            // Connection string to MySQL
            string connectionString = Configuration.GetConnectionString("dbconn");
            // Register the connection string as a singleton
            services.AddSingleton(connectionString);
            services.AddStackExchangeRedisCache(options =>
            {
                //options.Configuration = "10.20.21.25:6379,password=Trust@@$$Banc_COOperate**#%%$$Group";
                //options.InstanceName = "Prime";
                options.Configuration = appSetting.GetValue<string>("RedisIPAndPassword");
                options.InstanceName = appSetting.GetValue<string>("RedisInstanceName"); ;
            });
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60); // Session expires after 60 minutes
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            // Register Quartz services
            
            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionScopedJobFactory();
                var jobKey = new JobKey("BirthdayGreetingJob");
                q.AddJob<BirthdayGreetingJob>(opts => opts.WithIdentity(jobKey));
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("BirthdayGreetingJob-trigger")
                    .WithCronSchedule("0 0 6 * * ?")); // 6 AM daily
            });
            
            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
            // Register the job with the connection string
            
            services.AddTransient<BirthdayGreetingJob>(_ => new BirthdayGreetingJob(connectionString, _.GetRequiredService<ISmsBLService>(), _.GetRequiredService<IGeneric>()));
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "RetailBanking Asset Management API", Version = "v1" });
                //  c.OperationFilter<SwaggerFileOperationFilter>();
                c.SchemaFilter<SwaggerIgnoreFilter>();
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseSession(); // Enable session middleware
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "RetailBanking Asset_Capital_Insurance Management Service v2");
            });
        }
    }
}
