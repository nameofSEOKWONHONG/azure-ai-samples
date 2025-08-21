using Document.Intelligence.Agent.Features.Drm.M365.Models;
using eXtensionSharp;
using Microsoft.Extensions.Options;
using Microsoft.InformationProtection;

namespace Document.Intelligence.Agent.Features.Drm.M365;

public class DrmHandler : IDrmHandler
{
    //private string clientId => _configuration["ida:ClientId"];
    //private string appName => _configuration["app:Name"];
    //private string appVersion => _configuration["app:Version"];
   // private string targetlableID => _configuration["mip:TargetLableId"];

    private DrmConfig _drmConfig;

    public DrmHandler(IOptions<DrmConfig> options)
    {
        _drmConfig = options.Value;
    }
    
    public async Task<LabelHandlerResult> ExecuteAsync(string src, string dest)
    {
        try
        {
            var appInfo = new ApplicationInfo()
            {
                // ApplicationId should ideally be set to the same ClientId found in the Azure AD App Registration.
                // This ensures that the clientID in AAD matches the AppId reported in AIP Analytics.
                ApplicationId = _drmConfig.IDA.CLIENT_ID,
                ApplicationName = _drmConfig.APP.NAME,
                ApplicationVersion = _drmConfig.APP.VERSION
            };

            var action = new Action(appInfo, _drmConfig);
            char[] separator = new char[] { ':' };
            string[] textArray1 = _drmConfig.MIP.TARGET_LABEL_ID.Split(separator);
            string name = textArray1[0];
            string str2 = textArray1[1];

            Action.FileOptions options = new Action.FileOptions
            {
                // 원본 파일명
                FileName = src,
                // 해제 후 파일명
                OutputName = dest,
                ActionSource = ActionSource.Manual,
                AssignmentMethod = AssignmentMethod.Auto,
                DataState = DataState.Rest,
                GenerateChangeAuditEvent = true,
                IsAuditDiscoveryEnabled = true,
                // 변경할 라벨 ID
                LabelId = str2
            };

            // 현재 라벨 정보 가져오기
            ContentLabel oldLabel = await action.GetLabel(options);

            if (oldLabel.xIsEmpty() ||
                !oldLabel.IsProtectionAppliedFromLabel)
            {
                // 현재 라벨이 없거나 보호가 적용되지 않은 경우
                return new LabelHandlerResult(true, false, src);
            }

            var result = await action.SetLabel(options);
            if(result)
            {
                // 라벨 변경 성공
                return new LabelHandlerResult(true, true, dest);
            }

        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            Console.WriteLine($"An error occurred: {ex.Message}");
            return new LabelHandlerResult(false, ex.Message);
        }

        return new LabelHandlerResult(false, "Unknown");
    }
}