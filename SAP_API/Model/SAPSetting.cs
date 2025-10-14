namespace SAP_API.Model
{
    public class SAPSetting
    {
        public string Server { get; set; }
        public string LicenseServer { get; set; }
        public string DbServerType { get; set; }
        public string CompanyDB { get; set; }
        public string DbUserName { get; set; }
        public string DbPassword { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool UseTrusted { get; set; }
    }
    public class APISetting
    {
        public string BaseUrl { get; set; }
        public string CompanyDB { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string CashAccount { get; set; }
    }
    public class SAPSERVER
    {
        public string SapServer { get; set; }
        public string SapCompanyDB { get; set; }
        public string SapUserName { get; set; }
        public string SapPassword { get; set; }
        public string SapDbUserName { get; set; }
        public string SapDbPassword { get; set; }
        public string SapLicenseServer { get; set; }
        public string CashAccount { get; set; }
    }
    public class APISyn
    {
        public string BaseUrl { get; set; }
    }
    public class APIEcomaint
    {
        public string BaseUrl { get; set; }
        public string Time { get; set; }
    }
    public class InvoiceResponse
    {
        public List<InvoiceValue> Value { get; set; }
    }

    public class InvoiceValue
    {
        public int DocEntry { get; set; }
        public string OdataEtag { get; set; }
    }
    public class Cookies
    {
        public string B1SESSION { get; set; }
        public string ROUTEID { get; set; }
        public DateTime SessionTime { get; set; }
    }
}
