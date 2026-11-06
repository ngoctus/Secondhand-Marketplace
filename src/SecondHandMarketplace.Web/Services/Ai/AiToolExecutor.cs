using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Products;

namespace SecondHandMarketplace.Web.Services.Ai;

/// <summary>Kết quả chạy một công cụ, được gửi lại cho AI dưới dạng tool_result.</summary>
public sealed record AiToolResult(bool IsError, string Json)
{
    public static AiToolResult Error(string message) =>
        new(true, JsonSerializer.Serialize(new { error = message }, AiToolExecutor.JsonOptions));
}

/// <summary>
/// Danh sách CỐ ĐỊNH các công cụ AI được phép gọi và cách chạy chúng.
/// - AI không có đường nào chạy SQL hay đọc database tùy ý: mọi truy vấn đi qua IProductService / ICategoryService hiện có.
/// - Tham số do AI sinh ra được xem là không đáng tin: kiểm tra kiểu, độ dài, miền giá trị trước khi dùng.
/// - Tên công cụ không có trong danh sách (hoặc không được phép theo vai trò) bị từ chối.
/// - Không công cụ nào ghi dữ liệu: tìm kiếm/gợi ý chỉ đọc, viết mô tả chỉ trả thông tin để AI soạn văn bản.
/// </summary>
public sealed class AiToolExecutor
{
    public const string SearchProducts = "search_products";
    public const string RecommendProducts = "recommend_products";
    public const string DraftProductDescription = "draft_product_description";

    public const int KeywordMaxLength = 100;
    public const int NameMaxLength = 150;
    public const int FreeTextMaxLength = 1500;
    private const decimal PriceMaxValue = 1_000_000_000_000m;
    private const int DescriptionSnippetLength = 300;

    // USER (người bán/mua) dùng đủ ba chức năng; ADMIN chỉ tra cứu sản phẩm, không soạn mô tả đăng bán.
    private static readonly IReadOnlyDictionary<string, string[]> ToolsByRole = new Dictionary<string, string[]>
    {
        [AppRoles.User] = new[] { SearchProducts, RecommendProducts, DraftProductDescription },
        [AppRoles.Admin] = new[] { SearchProducts, RecommendProducts }
    };

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // JSON chỉ gửi tới API (không nhúng vào HTML) nên giữ nguyên tiếng Việt, đỡ tốn token.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IProductService _products;
    private readonly ICategoryService _categories;

    public AiToolExecutor(IProductService products, ICategoryService categories)
    {
        _products = products;
        _categories = categories;
    }

    public static bool IsAllowed(string role, string toolName) =>
        ToolsByRole.TryGetValue(role, out var tools) && tools.Contains(toolName, StringComparer.Ordinal);

    /// <summary>Định nghĩa công cụ (theo định dạng tools của Messages API) mà vai trò này được phép dùng.</summary>
    public static JsonArray BuildToolDefinitions(string role)
    {
        var array = new JsonArray();
        if (!ToolsByRole.TryGetValue(role, out var tools)) return array;
        foreach (var name in tools) array.Add(Definition(name));
        return array;
    }

    public async Task<AiToolResult> ExecuteAsync(string role, string toolName, JsonElement input, CancellationToken cancellationToken)
    {
        if (!IsAllowed(role, toolName))
            return AiToolResult.Error("Công cụ không tồn tại hoặc không được phép sử dụng.");

        return toolName switch
        {
            SearchProducts => await SearchAsync(input, cancellationToken),
            RecommendProducts => await RecommendAsync(input, cancellationToken),
            DraftProductDescription => DraftDescription(input),
            _ => AiToolResult.Error("Công cụ không tồn tại hoặc không được phép sử dụng.")
        };
    }

    // ------------------------------------------------------------------ 1. Tìm kiếm sản phẩm

    private async Task<AiToolResult> SearchAsync(JsonElement args, CancellationToken ct)
    {
        var (filter, error) = await BuildFilterAsync(args, ct);
        if (filter is null) return AiToolResult.Error(error!);

        var limit = ReadLimit(args, defaultValue: 6, max: 10);
        var found = await _products.SearchAsync(filter);

        var items = found.Items;
        var shown = items.Take(limit).Select(ToItem).ToList();
        return Ok(new
        {
            matchedCount = found.TotalItems,
            shownCount = shown.Count,
            products = shown,
            note = found.TotalItems == 0
                ? "Không có sản phẩm nào khớp. Hãy báo rõ điều này; có thể thử từ khóa khác hoặc nới bộ lọc."
                : null
        });
    }

    // ------------------------------------------------------------------ 2. Gợi ý sản phẩm

    private async Task<AiToolResult> RecommendAsync(JsonElement args, CancellationToken ct)
    {
        var (filter, error) = await BuildFilterAsync(args, ct);
        if (filter is null) return AiToolResult.Error(error!);

        var limit = ReadLimit(args, defaultValue: 5, max: 8);
        var found = await _products.SearchAsync(filter);

        // Lấy thêm mô tả thật của từng ứng viên để AI so sánh; vẫn chỉ đọc qua service hiện có.
        var candidates = new List<object>();
        foreach (var card in found.Items.Take(limit))
        {
            ct.ThrowIfCancellationRequested();
            var detail = await _products.GetDetailAsync(card.Id);
            candidates.Add(new
            {
                id = card.Id,
                name = card.Name,
                priceVnd = card.Price,
                category = card.CategoryName,
                condition = card.ConditionName,
                shop = card.ShopName,
                url = ProductUrl(card.Id),
                description = Truncate(detail?.Description, DescriptionSnippetLength)
            });
        }

        return Ok(new
        {
            matchedCount = found.TotalItems,
            candidateCount = candidates.Count,
            candidates,
            note = candidates.Count == 0
                ? "Hiện chưa có sản phẩm phù hợp trong hệ thống. Hãy nói rõ điều này, không tự nghĩ ra sản phẩm."
                : "Chỉ được gợi ý trong danh sách candidates, nêu đúng giá và thông tin như trên."
        });
    }

    // ------------------------------------------------------------------ 3. Viết mô tả sản phẩm

    /// <summary>
    /// Không truy cập database và không ghi gì. Công cụ kiểm tra thông tin người dùng cung cấp rồi trả lại đúng các thông tin đó
    /// kèm quy tắc, để AI soạn mô tả mà không thêm dữ kiện nào ngoài dữ liệu đầu vào.
    /// </summary>
    private static AiToolResult DraftDescription(JsonElement args)
    {
        var name = ReadString(args, "name", NameMaxLength);
        if (string.IsNullOrEmpty(name))
            return AiToolResult.Error("Thiếu tên sản phẩm. Hãy hỏi người dùng tên sản phẩm cần viết mô tả.");

        var category = ReadString(args, "category", KeywordMaxLength);
        var condition = ReadString(args, "condition", KeywordMaxLength);
        var details = ReadString(args, "details", FreeTextMaxLength);
        var currentDescription = ReadString(args, "current_description", FreeTextMaxLength);

        if (!TryReadDecimal(args, "price", out var price, out var priceError))
            return AiToolResult.Error(priceError!);

        if (category is null && condition is null && details is null && currentDescription is null && price is null)
            return AiToolResult.Error("Chưa có đủ thông tin để viết mô tả. Hãy hỏi người dùng thêm tình trạng, đặc điểm hoặc mô tả hiện có.");

        var provided = new Dictionary<string, object>();
        provided["name"] = name;
        if (category is not null) provided["category"] = category;
        if (condition is not null) provided["condition"] = condition;
        if (price is not null) provided["priceVnd"] = price.Value;
        if (details is not null) provided["details"] = details;
        if (currentDescription is not null) provided["currentDescription"] = currentDescription;

        return Ok(new
        {
            provided,
            instructions = new[]
            {
                "Viết mô tả đăng bán bằng tiếng Việt, rõ ràng, dễ đọc, dựa CHỈ trên các thông tin trong 'provided'.",
                "Không thêm bất kỳ thông số, tình trạng, phụ kiện, bảo hành, lý do bán hay cam kết nào không có trong 'provided'.",
                "Nếu thiếu thông tin quan trọng, nêu ngắn gọn cần bổ sung gì thay vì tự bịa.",
                "Đây chỉ là bản nháp để người dùng tự chỉnh và tự đăng; bạn không đăng hay sửa sản phẩm."
            }
        });
    }

    // ------------------------------------------------------------------ Bộ lọc dùng chung (tìm kiếm / gợi ý)

    private async Task<(ProductSearchVm? Filter, string? Error)> BuildFilterAsync(JsonElement args, CancellationToken ct)
    {
        var filter = new ProductSearchVm { Keyword = ReadString(args, "keyword", KeywordMaxLength) };

        if (!TryReadDecimal(args, "min_price", out var min, out var minError)) return (null, minError);
        if (!TryReadDecimal(args, "max_price", out var max, out var maxError)) return (null, maxError);
        if (min is not null && max is not null && min > max)
            return (null, "min_price không được lớn hơn max_price.");
        filter.MinPrice = min;
        filter.MaxPrice = max;

        var categoryName = ReadString(args, "category", KeywordMaxLength);
        if (categoryName is not null)
        {
            var categories = (await _categories.GetCategoriesAsync(ct)).Where(c => c.DangHienThi).ToList();
            var match = FindByName(categories, c => c.Ten, categoryName);
            if (match is null)
                return (null, $"Không có danh mục '{categoryName}'. Danh mục hiện có: {string.Join(", ", categories.Select(c => c.Ten))}.");
            filter.CategoryId = match.MaDanhMuc;
        }

        var conditionName = ReadString(args, "condition", KeywordMaxLength);
        if (conditionName is not null)
        {
            var conditions = (await _categories.GetConditionsAsync(ct)).Where(c => c.DangSuDung).ToList();
            var match = FindByName(conditions, c => c.Ten, conditionName);
            if (match is null)
                return (null, $"Không có tình trạng '{conditionName}'. Tình trạng hiện có: {string.Join(", ", conditions.Select(c => c.Ten))}.");
            filter.ConditionId = match.MaTinhTrang;
        }

        return (filter, null);
    }

    /// <summary>So khớp tên không phân biệt hoa thường và dấu tiếng Việt: khớp đúng trước, rồi khớp chứa nhau.</summary>
    private static T? FindByName<T>(IReadOnlyList<T> items, Func<T, string> nameOf, string wanted) where T : class
    {
        var key = TextHelper.ChuanHoa(wanted);
        if (key.Length == 0) return null;

        var exact = items.FirstOrDefault(x => TextHelper.ChuanHoa(nameOf(x)) == key);
        if (exact is not null) return exact;

        return items.FirstOrDefault(x =>
        {
            var n = TextHelper.ChuanHoa(nameOf(x));
            return n.Length > 0 && (n.Contains(key) || key.Contains(n));
        });
    }

    // ------------------------------------------------------------------ Đọc tham số an toàn

    private static bool TryGetArg(JsonElement args, string name, out JsonElement value)
    {
        value = default;
        return args.ValueKind == JsonValueKind.Object
               && args.TryGetProperty(name, out value)
               && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);
    }

    private static string? ReadString(JsonElement args, string name, int maxLength)
    {
        if (!TryGetArg(args, name, out var value)) return null;
        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
        text = text?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        text = RemoveControlCharacters(text);
        return text.Length > maxLength ? text[..maxLength] : text;
    }

    private static bool TryReadDecimal(JsonElement args, string name, out decimal? value, out string? error)
    {
        value = null;
        error = null;
        if (!TryGetArg(args, name, out var raw)) return true;

        decimal parsed;
        var ok = raw.ValueKind switch
        {
            JsonValueKind.Number => raw.TryGetDecimal(out parsed),
            JsonValueKind.String => decimal.TryParse(raw.GetString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsed),
            _ => Fail(out parsed)
        };

        if (!ok || parsed < 0 || parsed > PriceMaxValue)
        {
            error = $"Giá trị '{name}' không hợp lệ: cần là số VND từ 0 trở lên.";
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool Fail(out decimal parsed)
    {
        parsed = 0;
        return false;
    }

    private static int ReadLimit(JsonElement args, int defaultValue, int max)
    {
        if (!TryGetArg(args, "limit", out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var limit))
            return defaultValue;
        return Math.Clamp(limit, 1, max);
    }

    // ------------------------------------------------------------------ Tiện ích

    private static object ToItem(ProductCardVm p) => new
    {
        id = p.Id,
        name = p.Name,
        priceVnd = p.Price,
        category = p.CategoryName,
        condition = p.ConditionName,
        shop = p.ShopName,
        url = ProductUrl(p.Id)
    };

    private static string ProductUrl(int id) => $"/Products/Details/{id}";

    private static string? Truncate(string? text, int maxLength)
    {
        text = text?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        return text.Length > maxLength ? text[..maxLength] + "…" : text;
    }

    private static string RemoveControlCharacters(string text) =>
        new(text.Where(c => !char.IsControl(c) || c is '\n' or '\t').ToArray());

    private static AiToolResult Ok(object payload) => new(false, JsonSerializer.Serialize(payload, JsonOptions));

    // ------------------------------------------------------------------ Định nghĩa công cụ gửi cho AI

    private static JsonObject Definition(string name) => name switch
    {
        SearchProducts => Parse("""
            {
              "name": "search_products",
              "description": "Tìm sản phẩm đang được bán trên Re:market theo từ khóa và bộ lọc. Chỉ trả về sản phẩm có thật trong hệ thống. Dùng khi người dùng muốn tìm một món đồ cụ thể.",
              "input_schema": {
                "type": "object",
                "properties": {
                  "keyword": { "type": "string", "description": "Từ khóa ngắn 1-4 từ, ví dụ: iphone 12, bàn học, giày chạy bộ. Không đưa giá hay tình trạng vào từ khóa." },
                  "category": { "type": "string", "description": "Tên danh mục nếu người dùng nêu rõ." },
                  "condition": { "type": "string", "description": "Tình trạng sản phẩm nếu người dùng nêu rõ, ví dụ: Như mới." },
                  "min_price": { "type": "number", "description": "Giá tối thiểu, đơn vị VND." },
                  "max_price": { "type": "number", "description": "Giá tối đa, đơn vị VND." },
                  "limit": { "type": "integer", "description": "Số kết quả tối đa, từ 1 đến 10 (mặc định 6)." }
                }
              }
            }
            """),
        RecommendProducts => Parse("""
            {
              "name": "recommend_products",
              "description": "Lấy các sản phẩm thật đang bán, kèm mô tả, để gợi ý theo nhu cầu của người dùng (ví dụ ngân sách, mục đích sử dụng). Chỉ được gợi ý trong số sản phẩm công cụ này trả về.",
              "input_schema": {
                "type": "object",
                "properties": {
                  "keyword": { "type": "string", "description": "Từ khóa ngắn 1-4 từ mô tả loại đồ cần tìm. Bỏ trống nếu chỉ lọc theo danh mục hoặc ngân sách." },
                  "category": { "type": "string", "description": "Tên danh mục nếu có." },
                  "condition": { "type": "string", "description": "Tình trạng mong muốn nếu có." },
                  "min_price": { "type": "number", "description": "Ngân sách tối thiểu, VND." },
                  "max_price": { "type": "number", "description": "Ngân sách tối đa, VND." },
                  "limit": { "type": "integer", "description": "Số ứng viên tối đa, từ 1 đến 8 (mặc định 5)." }
                }
              }
            }
            """),
        DraftProductDescription => Parse("""
            {
              "name": "draft_product_description",
              "description": "Nhận thông tin sản phẩm do người dùng cung cấp để soạn mô tả đăng bán. Chỉ dùng thông tin người dùng đã đưa; không đăng hay sửa sản phẩm.",
              "input_schema": {
                "type": "object",
                "properties": {
                  "name": { "type": "string", "description": "Tên sản phẩm (bắt buộc)." },
                  "category": { "type": "string", "description": "Loại sản phẩm nếu người dùng nói." },
                  "condition": { "type": "string", "description": "Tình trạng nếu người dùng nói." },
                  "price": { "type": "number", "description": "Giá bán VND nếu người dùng nói." },
                  "details": { "type": "string", "description": "Đặc điểm, thông số, phụ kiện... mà người dùng cung cấp." },
                  "current_description": { "type": "string", "description": "Mô tả hiện có mà người dùng muốn viết lại." }
                },
                "required": ["name"]
              }
            }
            """),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Công cụ không xác định.")
    };

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();
}
