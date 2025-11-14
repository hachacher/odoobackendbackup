using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OdooBackend.Services
{
    public class OdooService
    {
        private readonly HttpClient _httpClient;
        private int _uid;
        private string _sessionId;

        // Odoo credentials
        private const string Db = "plennix-we-fashion-stage-3-25274609";
        private const string Username = "admin";
        private const string Password = "P@s$w0rd@2025"; // put secure way later

        public OdooService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task InitializeAsync()
        {
            var payload = new
            {
                jsonrpc = "2.0",
                method = "call",
                service = "common",
                @params = new { db = Db, login = Username, password = Password }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/web/session/authenticate", content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resultJson);

            if (doc.RootElement.TryGetProperty("result", out var resultElement) &&
                resultElement.TryGetProperty("uid", out var uidProp))
            {
                _uid = uidProp.GetInt32();
                // Remove _sessionId completely
                // _sessionId = resultElement.GetProperty("session_id").GetString();
            }
            else
            {
                throw new Exception("Odoo authentication failed");
            }
        }


        // Step 1: Search variant by barcode
        public async Task<int?> SearchProductVariantByBarcodeAsync(string barcode)
        {
            var payload = new
            {
                jsonrpc = "2.0",
                method = "call",
                service = "object",
                @params = new
                {
                    model = "product.product",
                    method = "search_read",
                    args = new object[]
                    {
                        new object[] { new object[] { "barcode", "=", barcode } }
                    },
                    kwargs = new { fields = new[] { "id", "name" }, limit = 1 }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("X-Openerp-Session-Id", _sessionId);

            var response = await _httpClient.PostAsync("/web/dataset/call_kw", content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resultJson);

            if (doc.RootElement.TryGetProperty("result", out var resultElement) &&
                resultElement.ValueKind == JsonValueKind.Array &&
                resultElement.GetArrayLength() > 0 &&
                resultElement[0].TryGetProperty("id", out var idProp))
            {
                return idProp.GetInt32();
            }

            return null;
        }

        // Step 2: Get first active pricelist (default for PHOENIX company)
        public async Task<int> GetDefaultPricelistAsync()
        {
            var payload = new
            {
                jsonrpc = "2.0",
                method = "call",
                service = "object",
                @params = new
                {
                    model = "product.pricelist",
                    method = "search_read",
                    args = new object[]
                    {
                        new object[] { new object[] { "active", "=", true } }
                    },
                    kwargs = new { fields = new[] { "id" }, limit = 1 }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("X-Openerp-Session-Id", _sessionId);

            var response = await _httpClient.PostAsync("/web/dataset/call_kw", content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resultJson);

            if (doc.RootElement.TryGetProperty("result", out var resultElement) &&
                resultElement.ValueKind == JsonValueKind.Array &&
                resultElement.GetArrayLength() > 0 &&
                resultElement[0].TryGetProperty("id", out var idProp))
            {
                return idProp.GetInt32();
            }

            throw new Exception("No active pricelist found");
        }

        // Step 3: Compute price with pricelist
        public async Task<decimal?> ComputePriceWithPricelistAsync(int productId, int pricelistId)
        {
            var payload = new
            {
                jsonrpc = "2.0",
                method = "call",
                service = "object",
                @params = new
                {
                    model = "product.product",
                    method = "price_compute",
                    args = new object[]
                    {
                new int[] { productId },       // flat array of product IDs
                new string[] { "list_price" }  // price types
                    },
                    kwargs = new
                    {
                        context = new
                        {
                            pricelist = pricelistId,
                            qty = 1
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("X-Openerp-Session-Id", _sessionId);

            var response = await _httpClient.PostAsync("/web/dataset/call_kw", content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resultJson);

            if (doc.RootElement.TryGetProperty("result", out var resultElement))
            {
                // price_compute returns a dict keyed by product ID
                if (resultElement.TryGetProperty(productId.ToString(), out var productDict) &&
                    productDict.TryGetProperty("list_price", out var priceElement))
                {
                    return priceElement.GetDecimal();
                }
            }

            return null;
        }









        // High-level: get sales price by barcode
        public async Task<decimal?> GetSalesPriceByBarcodeAsync(string barcode)
        {
            var productId = await SearchProductVariantByBarcodeAsync(barcode);
            if (productId == null) return null;

            var pricelistId = await GetDefaultPricelistAsync();
            return await ComputePriceWithPricelistAsync(productId.Value, pricelistId);
        }
    }
}
