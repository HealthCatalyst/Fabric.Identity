using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;

namespace Fabric.Identity.API.Services
{
    public interface ICertificateService
    {
        X509Certificate2 GetCertificate(SigningCertificateSettings certificateSettings, bool isPrimary = true);
    }
}
