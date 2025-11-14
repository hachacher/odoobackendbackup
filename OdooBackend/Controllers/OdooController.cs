using Microsoft.AspNetCore.Mvc;
using OdooBackend.Services; 

namespace OdooBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OdooController : ControllerBase
    {
        private readonly OdooService _odooService;

        public OdooController(OdooService odooService)
        {
            _odooService = odooService;
        }

        [HttpGet("getprice")]
        public async Task<IActionResult> GetPrice([FromQuery] string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return BadRequest("Barcode is required.");

            try
            {
                var price = await _odooService.GetSalesPriceByBarcodeAsync(barcode);
                if (price == null)
                    return NotFound(new { message = "Product not found in PHOENIX company." });

                return Ok(new
                {
                    company = "PHOENIX",
                    barcode,
                    price
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}