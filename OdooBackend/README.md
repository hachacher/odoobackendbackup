# OdooBackend

## GetItemByBarcodeAsync pricing logic
`OdooClient.GetItemByBarcodeAsync` now returns an `OdooItem` with two price fields:

- `OriginalPrice`: The product's `list_price` (never modified).
- `DiscountedPrice`: A computed discounted price if a valid pricelist rule applies; otherwise `null`.

### Pricelist rule selection
The method fetches one pricelist item (`product.pricelist.item`) in two passes:
1. Variant-specific rule (`applied_on = '0_product_variant'` and `product_id = <variant id>`)
2. Fallback template rule (`applied_on = '1_product'` and `product_tmpl_id = <template id>`) if no variant rule is found.

Returned fields now include `date_start` and `date_end` so validity can be checked locally.

### Date validity
A rule is considered valid if:
- `date_start` is empty OR `date_start <= UtcNow`
- `date_end` is empty OR `date_end >= UtcNow`

If the rule is not within this window it is ignored.

### Discount computation
Depending on `compute_price`:
- `fixed`: `DiscountedPrice = fixed_price`
- `percentage`: `DiscountedPrice = OriginalPrice - (OriginalPrice * percent_price / 100)`

Other `compute_price` modes (e.g. `formula`) are currently not implemented; extend in the switch logic if needed.

### Edge cases
- If `list_price` is missing, both price fields remain `null`.
- If multiple valid rules exist the first returned by Odoo (ordered by `min_quantity desc`) is applied.
- If date parsing fails the rule is treated as having no date constraint (Odoo sometimes returns empty strings).

### Next improvements
- Support `formula` compute mode.
- Narrow the XML-RPC domain to pre-filter by date (currently filtered client-side for simplicity).
- Add unit tests mocking XML-RPC layer.
