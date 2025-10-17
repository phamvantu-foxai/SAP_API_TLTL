using Microsoft.Extensions.Options;
using SAP_API.Model;
using SAPbobsCOM;
using System.Net.Http;

namespace SAP_API.Service
{
    public class SAPConnection
    {
        private SAPbobsCOM.Company _company;
        private readonly SAPSERVER _settings;

        public SAPConnection(IOptions<SAPSERVER> settings)
        {
            _settings = settings.Value;
        }

        public SAPbobsCOM.Company Company => _company;

        public bool Connect()
        {
            if (_company != null && _company.Connected)
                return true;

            _company = new SAPbobsCOM.Company
            {
                Server = _settings.SapServer,
                CompanyDB = _settings.SapCompanyDB,
                DbServerType = SAPbobsCOM.BoDataServerTypes.dst_MSSQL2019,
                DbUserName = _settings.SapDbUserName,
                DbPassword = _settings.SapDbPassword,
                LicenseServer = _settings.SapLicenseServer,
                language = SAPbobsCOM.BoSuppLangs.ln_English,
                UseTrusted = false,
                UserName = _settings.SapUserName,
                Password = _settings.SapPassword
            };

            int result = _company.Connect();
            if (result != 0)
            {
                string errMsg = _company.GetLastErrorDescription();
                Console.WriteLine($"❌ SAP connection failed: {errMsg}");
                return false;
            }

            return true;
        }
    }
}
