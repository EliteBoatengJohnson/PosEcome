using PosSystem.SharedKernel;
using PosSystem.Modules.Products.Models;

namespace PosSystem.Modules.Products.Services;

public interface IProductService
{
    Task<Result<PagedResult<ProductDto>>> GetAllAsync(int page, int pageSize, Guid? categoryId, string? search,
        CancellationToken ct = default);

    Task<Result<ProductDto>> CreateAsync(CreateProductRequest req, CancellationToken ct = default);
    
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest req, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProductDto>> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<Result<int>> BulkImportAsync(Stream csvStream, CancellationToken ct = default);
    Task<Result<byte[]>> ExportAsync(CancellationToken ct = default);
    Task<Result<List<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default);
    Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest req, CancellationToken ct = default);
}