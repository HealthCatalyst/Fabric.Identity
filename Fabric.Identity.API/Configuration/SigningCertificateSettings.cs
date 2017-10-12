namespace Fabric.Identity.API.Configuration
{
    public class SigningCertificateSettings
    {
        public bool UseTemporarySigningCredential { get; set; }
        public string PrimaryCertificateThumbprint { get; set; }
        public string SecondaryCertificateThumbprint { get; set; }
        public string EncryptionCertificateThumbprint { get; set; }
        public string PrimaryCertificatePath { get; set; }
        public string SecondaryCertificatePath { get; set; }
        public string PrimaryCertificatePasswordPath { get; set; }
        public string SecondaryCertificatePasswordPath { get; set; }
        
    }
}
