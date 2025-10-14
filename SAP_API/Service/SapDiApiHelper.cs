using SAP_API.Model;
using SAPbobsCOM;

namespace SAP_API.Service
{
    public class SAPConnection
    {
        private static SAPbobsCOM.Company company;
        public static SAPbobsCOM.Company SapCompany { get { return company; } }
        public static bool Connect()
        {
            company = new SAPbobsCOM.Company(); ;
            company.Server = ConfigurationManager.["SapServer"].ToString();
            company.CompanyDB = ConfigurationManager.AppSettings["SapCompanyDB"].ToString();
            company.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_MSSQL2019;
            company.DbUserName = ConfigurationManager.AppSettings["SapDbUserName"].ToString();
            company.DbPassword = ConfigurationManager.AppSettings["SapDbPassword"].ToString();
            company.language = SAPbobsCOM.BoSuppLangs.ln_English;
            company.UseTrusted = false;
            company.UserName = ConfigurationManager.AppSettings["SapUserName"].ToString();
            company.Password = ConfigurationManager.AppSettings["SapPassword"].ToString();

            if (company.Connected == true)
                return true;
            else
            {
                if (company.Connect() != 0)
                    return false;
            }
            return true;
        }


    }
}
