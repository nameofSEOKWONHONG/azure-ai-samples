using System.Security.Claims;
using eXtensionSharp;
using Microsoft.AspNetCore.Http;

namespace Document.Intelligence.Agent.Infrastructure.Session;

/// <summary>
/// DOCUMENT INTELLIGENCE AGENT SESSION CONTEXT
/// </summary>
public interface IDiaSessionContext
{
    bool? IsAdmin { get; }
    string UserId { get; }
    string Email { get; }
    DateTime GetNow();
    DateOnly GetNowOnly();
    string SelectedLanguage { get; }
}

/// <summary>
/// implement
/// </summary>
public class DiaSessionContext: IDiaSessionContext
{
    public bool? IsAdmin { get; }
    public string UserId { get; }
    public string Email { get; }
    public string SelectedLanguage { get; }
    public DiaSessionContext(IHttpContextAccessor accessor)
    {
        //콘솔 프로그램 예외 처리
        if (accessor.HttpContext.xIsNotEmpty())
        {
            this.IsAdmin = accessor.HttpContext.User?.FindAll(ClaimTypes.Role).Any(m => m.Value.ToUpper() == "ADMIN");
            this.UserId = accessor.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            this.Email = accessor.HttpContext.User?.FindFirst(ClaimTypes.Email)?.Value;    
        }

        #if DEBUG
        if (!accessor.HttpContext.User.Identity.IsAuthenticated)
        {
            this.IsAdmin = true;
            this.UserId = Guid.NewGuid().ToString();
            this.Email = "test@test.com";
        }
        #endif
        this.SelectedLanguage = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
    }
    
    public DateTime GetNow()
    {
        TimeZoneInfo koreaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, koreaTimeZone);
    }
    
    public DateOnly GetNowOnly()
    {
        return DateOnly.FromDateTime(GetNow());
    }

    public DateOnly ConvertDateTimeToDateOnly(DateTime? dateTime)
    {
        if (dateTime == null)
        {
            return GetNowOnly();
        }
        return DateOnly.FromDateTime(dateTime.Value);
    }
}