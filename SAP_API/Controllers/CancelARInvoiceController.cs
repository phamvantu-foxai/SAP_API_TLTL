using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SAP_API.Model;
using SAP_API.Service;

namespace SAP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CancelARInvoiceController : Controller
    {
        private readonly SapInvoiceService _sapService;
        private readonly IOptions<APISetting> _api;

        public CancelARInvoiceController(SapInvoiceService sapService, IOptions<APISetting> api)
        {
            _api = api;
            _sapService = sapService;
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> addARInvoice(CancelARInvoice ar)
        {

            var mess = await _sapService.CreateCancelInvoiceWithPaymentDIAPIAsync(ar);
            return Ok(mess);
        }
    }
}
