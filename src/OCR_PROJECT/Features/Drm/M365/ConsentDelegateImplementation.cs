using Microsoft.InformationProtection;

namespace Document.Intelligence.Agent.Features.Drm.M365
{
    internal class ConsentDelegateImplementation : IConsentDelegate
    {
        public Consent GetUserConsent(string url)
        {
            return Consent.Accept;
        }
    }
}
