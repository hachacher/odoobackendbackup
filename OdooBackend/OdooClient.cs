using System;
using CookComputing.XmlRpc;
using OdooBackend.Interfaces;
using OdooBackend.Models;

namespace OdooBackend
{
    public class OdooClient
    {
        private readonly string _url;
        private readonly string _db;
        private readonly string _username;
        private readonly string _password;

        public OdooClient(string url, string db, string username, string password)
        {
            _url = url;
            _db = db;
            _username = username;
            _password = password;
        }

        public async Task<OdooItem?> GetItemByBarcodeAsync(string barcode)
        {
            try
            {
                var common = XmlRpcProxyGen.Create<IOdooCommon>();
                
                common.Url = $"{_url}/xmlrpc/2/common";

                var uid = common.Authenticate(_db, _username, _password, new { });
                if (uid == 0) return null;

                var objects = XmlRpcProxyGen.Create<IOdooObject>();
                objects.Url = $"{_url}/xmlrpc/2/object";

                // 1) Lookup company 'PHOENIX'
                int? companyId = null;
                try
                {
                    var companyDomain = new object[] { new object[] { "name", "=", "PHOENIX" } };
                    var companyFields = new object[] { "id" };
                    var companyKwargs = new { fields = companyFields, limit = 1 };
                    var companyResult = objects.SearchRead(_db, uid, _password, "res.company", "search_read", new object[] { companyDomain }, companyKwargs);
                    if (companyResult.Length > 0 && companyResult[0] is XmlRpcStruct companyStruct && companyStruct.ContainsKey("id"))
                    {
                        companyId = Convert.ToInt32(companyStruct["id"]);
                    }
                }
                catch (Exception ce)
                {
                    Console.WriteLine($"Company lookup failed: {ce.Message}");
                }

                if (companyId == null)
                {
                    Console.WriteLine("Company 'PHOENIX' not found. Aborting item lookup.");
                    return null; // Or continue without company context if desired
                }

                // 2) Find product by barcode with company context
                var domain = new object[] { new object[] { "barcode", "=", barcode } };
                var fields = new object[] { "id", "default_code", "name", "list_price", "product_template_attribute_value_ids", "product_tmpl_id" };
                var context = new { allowed_company_ids = new int[] { companyId.Value }, company_id = companyId.Value };
                var kwargs = new { fields = fields, limit = 1, context = context };

                var result = objects.SearchRead(_db, uid, _password,
                    "product.product", "search_read", new object[] { domain }, kwargs);

                if (result.Length == 0 || result[0] is not XmlRpcStruct product) return null;

                // Capture original (list) price and expose discounted separately
                var originalPrice = product.ContainsKey("list_price") ? Convert.ToDecimal(product["list_price"]) : (decimal?)null;

                var item = new OdooItem
                {
                    ItemNumber = product.ContainsKey("default_code") ? product["default_code"]?.ToString() : null,
                    Name = product.ContainsKey("name") ? product["name"]?.ToString() : null,
                    OriginalPrice = originalPrice,
                    DiscountedPrice = null, // will fill if we find a valid pricelist rule
                    Variants = new List<string>()
                };

                // 3) Attempt to override price from pricelist rules (product.pricelist.item)
                try
                {
                    int productId = product.ContainsKey("id") ? Convert.ToInt32(product["id"]) : 0;
                    object? tmplObj = product.ContainsKey("product_tmpl_id") ? product["product_tmpl_id"] : null;
                    int productTemplateId = 0;
                    // product_tmpl_id comes as int[] {id, display_name} or object[]
                    if (tmplObj is object[] arr && arr.Length > 0 && int.TryParse(arr[0]?.ToString(), out var tmplIdParsed))
                    {
                        productTemplateId = tmplIdParsed;
                    }

                    if (productId > 0 || productTemplateId > 0)
                    {
                        string today = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                        // Date validity domain (flattened)
                        // (date_start is null or <= today) AND (date_end is null or >= today)
                        // Equivalent to: (| (date_start = false) (date_start <= today)) AND (| (date_end = false) (date_end >= today))
                        var priceFields = new object[] { "compute_price", "fixed_price", "percent_price", "price_discount", "min_quantity", "applied_on", "date_start", "date_end" };
                        var priceKwargs = new { fields = priceFields, limit = 1, order = "min_quantity desc" };

                        // Domain for variant-specific search (flattened)
                        var priceDomainVariant = new object[] {
                            "&", "&", "&", "&",
                            "|", new object[] { "company_id", "=", false }, new object[] { "company_id", "=", companyId.Value },
                            new object[] { "applied_on", "=", "0_product_variant" },
                            new object[] { "product_id", "=", productId },
                            "|", new object[] { "date_start", "=", false }, new object[] { "date_start", "<=", today },
                            "|", new object[] { "date_end", "=", false }, new object[] { "date_end", ">=", today }
                        };

                        var variantRuleResult = objects.SearchRead(_db, uid, _password, "product.pricelist.item", "search_read", new object[] { priceDomainVariant }, priceKwargs);

                        XmlRpcStruct? rule = null;
                        if (variantRuleResult.Length > 0 && variantRuleResult[0] is XmlRpcStruct vr)
                        {
                            rule = vr;
                        }
                        else if (productTemplateId > 0)
                        {
                            // Fallback to template rule (flattened)
                            var priceDomainTemplate = new object[] {
                                "&", "&", "&", "&",
                                "|", new object[] { "company_id", "=", false }, new object[] { "company_id", "=", companyId.Value },
                                new object[] { "applied_on", "=", "1_product" },
                                new object[] { "product_tmpl_id", "=", productTemplateId },
                                "|", new object[] { "date_start", "=", false }, new object[] { "date_start", "<=", today },
                                "|", new object[] { "date_end", "=", false }, new object[] { "date_end", ">=", today }
                            };
                            var templateRuleResult = objects.SearchRead(_db, uid, _password, "product.pricelist.item", "search_read", new object[] { priceDomainTemplate }, priceKwargs);
                            if (templateRuleResult.Length > 0 && templateRuleResult[0] is XmlRpcStruct tr)
                            {
                                rule = tr;
                            }
                        }

                        if (rule != null && item.OriginalPrice.HasValue)
                        {
                            var computeMode = rule.ContainsKey("compute_price") ? rule["compute_price"]?.ToString() : null;
                            decimal listPrice = item.OriginalPrice.Value;

                            switch (computeMode)
                            {
                                case "fixed":
                                    if (rule.ContainsKey("fixed_price") && decimal.TryParse(rule["fixed_price"]?.ToString(), out var fixedPrice))
                                    {
                                        item.DiscountedPrice = fixedPrice;
                                    }
                                    break;
                                case "percentage": // This is for 'Discount' in UI
                                    if (rule.ContainsKey("price_discount") && decimal.TryParse(rule["price_discount"]?.ToString(), out var discountPercent))
                                    {
                                        item.DiscountedPrice = listPrice * (1 - (discountPercent / 100m));
                                    }
                                    break;
                                case "formula":
                                    // Formula evaluation is complex and requires a safe expression evaluator.
                                    // For now, we will not calculate the price but acknowledge it.
                                    // A full implementation would need to parse the formula and have access to variables like `product`, `user`, etc.
                                    Console.WriteLine("Formula-based pricelist item found, but calculation is not implemented.");
                                    break;
                            }
                        }
                    }
                }
                catch (CookComputing.XmlRpc.XmlRpcFaultException fault)
                {
                    Console.WriteLine($"Odoo pricelist XML-RPC fault {fault.FaultCode}: {fault.FaultString}");
                }
                catch (Exception priceEx)
                {
                    Console.WriteLine($"Pricelist price override failed: {priceEx.Message}");
                }

                // Step 4: Extract variant IDs
                if (product["product_template_attribute_value_ids"] is int[] variantIds && variantIds.Length > 0)
                {
                    var variantFields = new object[] { "name" };
                    var variantKwargs = new { fields = variantFields };

                    var variantResult = objects.SearchRead(_db, uid, _password,
                        "product.template.attribute.value", "read", new object[] { variantIds }, variantKwargs);

                    foreach (var variant in variantResult)
                    {
                        if (variant is XmlRpcStruct variantData && variantData.ContainsKey("name"))
                        {
                            var variantName = variantData["name"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(variantName))
                            {
                                item.Variants.Add(variantName);
                            }
                        }
                    }
                }

                return item;
            }
            catch(Exception e)
            {
                var s = e.Message;
            }
            return null;
        }
    }
}


