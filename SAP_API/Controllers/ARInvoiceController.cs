using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SAP_API.Model;
using SAP_API.Service;
using SAPbobsCOM;

namespace SAP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ARInvoiceController : Controller
    {
        private readonly SapInvoiceService _sapService;
        private readonly IOptions<APISetting> _api;

        public ARInvoiceController(SapInvoiceService sapService,IOptions<APISetting> api)
        {
            _api = api;
            _sapService = sapService;
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> addARInvoice(ARInvoice ar)
        {
            
            var mess = await  _sapService.CreateInvoiceWithPaymentDIAPIAsync(ar);
            return Ok(mess);
        }
    }
}
