namespace Fabric.Identity.API.Configuration
{
    public class SigningCertificateSettings
    {
        public bool UseTemporarySigningCredential { get; set; }
        public string PrimaryCertificateSubjectName { get; set; }
        public string SecondaryCertificateSubjectName { get; set; }
        
    }
}
