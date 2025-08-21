using Document.Intelligence.Agent.Features.Drm.M365.Models;

namespace Document.Intelligence.Agent.Features.Drm.M365;

public interface IDrmHandler
{
    /// <summary>
    /// M365에서는 라벨 변경.
    /// 다른 모듈도 해당 인터페이스 상속으로 구현한다.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="dest"></param>
    /// <returns></returns>
    Task<LabelHandlerResult> ExecuteAsync(string src, string dest);
}