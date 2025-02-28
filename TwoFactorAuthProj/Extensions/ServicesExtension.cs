using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TwoFactorAuthProj.Data;
using TwoFactorAuthProj.Entities;
using TwoFactorAuthProj.interfaces;
using TwoFactorAuthProj.Repositery;
using TwoFactorAuthProj.Services;

namespace TwoFactorAuthProj.Extensions;

public static class ServicesExtension
{
    public static void AddDb(this IServiceCollection services , IConfiguration config)
    {
        services.AddDbContext<DataContext>(opt => opt.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<AppUser>().
            AddEntityFrameworkStores<DataContext>();


        services.AddScoped<ISendEmailTotp,SendEmailTotp>();
        services.AddScoped<ISendSmsTotp,SendSmsTotp>();
        services.AddScoped<IOtpRepositery,OtpRepositery>();


        services.AddScoped<IOtpService,OtpService>();

        services.AddScoped<IOtpGoogleAuth,OtpGoogleAuth>();

        services.AddScoped<IDataBaseOtpService,DatabaseHmacOtp>();
    }

    public static void AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(policy =>
        {
            policy.AddPolicy("cors-policy", opt =>
            {
                opt.AllowAnyHeader().
                AllowCredentials().
                AllowAnyMethod().
                WithOrigins("http://localhost:4200", "https://localhost:4200");
            });
        });

    }


    public static void AddIoptions(this IServiceCollection services, IConfiguration config)
    {
    
    }
}
