namespace Document.Intelligence.Agent.Features.Drm.M365.Models;

public class DrmConfig
{
    public IdaConfig IDA { get; set; }
    public AppConfig APP { get; set; }
    public MipConfig MIP { get; set; }
}

public class IdaConfig
{
    public string TENANT { get; set; }
    public string CLIENT_ID { get; set; }
    public string CLIENT_SECRET { get; set; }
    public string REDIRECT_URI { get; set; }
    public string CERT_THUMB_PRINT { get; set; }
    public bool DO_CERT_AUTH { get; set; }
}

public class AppConfig
{
    public string NAME { get; set; }
    public string VERSION { get; set; }
}

public class MipConfig
{
    public string TARGET_LABEL_ID { get; set; }
    public string JUSTIFICATION_MESSAGE { get; set; }
    public string LOG_LEVEL { get; set; }
}
