using System;
using System.Collections;
using CookComputing.XmlRpc;
using Microsoft.AspNetCore.Mvc;
using OdooBackend.Interfaces;

namespace OdooBackend.Controllers
{ 
    [ApiController]
    [Route("[controller]")]
    public class PriceController : ControllerBase
    {
        private readonly OdooClient _odooClient;

        public PriceController()
        {
            // These should come from config/secrets
            _odooClient = new OdooClient(
                url: "https://plennix-we-fashion-stage-3-25274609.dev.odoo.com",
                db: "plennix-we-fashion-stage-3-25274609",
                username: "admin",
                password: "e8c660f5b941244428eeab0e4f4575960650f1c4");
        } 
        [HttpGet("{barcode}")]
        public async Task<IActionResult> GetPrice(string barcode)
        {
            var item = await _odooClient.GetItemByBarcodeAsync(barcode);
            if (item == null)
                return NotFound(new { message = "Item not found" });

            return Ok(item);
        }
    }


}

