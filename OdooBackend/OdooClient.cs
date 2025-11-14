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

                // Step 1: Find product by barcode
                var domain = new object[] { new object[] { "barcode", "=", barcode } };
                var fields = new object[] { "default_code", "name", "list_price", "product_template_attribute_value_ids" };
                var kwargs = new { fields = fields, limit = 1 };

                var result = objects.SearchRead(_db, uid, _password,
                    "product.product", "search_read", new object[] { domain }, kwargs);

                if (result.Length == 0 || result[0] is not XmlRpcStruct product) return null;

                var item = new OdooItem
                {
                    ItemNumber = product.ContainsKey("default_code") ? product["default_code"]?.ToString() : null,
                    Name = product.ContainsKey("name") ? product["name"]?.ToString() : null,
                    Price = product.ContainsKey("list_price") ? Convert.ToDecimal(product["list_price"]) : null,
                    Variants = new List<string>()
                };

                // Step 2: Extract variant IDs
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
                            item.Variants.Add(variantData["name"].ToString());
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

