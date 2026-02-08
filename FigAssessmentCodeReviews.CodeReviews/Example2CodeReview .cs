using Microsoft.AspNetCore.Mvc;

namespace FigAssessmentCodeReviews.CodeReviews
{
    /*
     * These are the things that I'm going to assume:
     * .NET version 8.0
     * We have DI configuration in our root file
     * We are not using global exception handling
    */

    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        // Can the query parameters be replaced with a query object? Query parameters will be more extensible/readable this way!
        // I love the flexibility of IActionResult, but since our return is always the same do you mind making a class for the return type? ActionResult<T> promotes type safety!
        // Would you also mind passing in a CancellationToken from here all the way to our SqlClient code? This will allow us to stop if the client cancels the request!
        [HttpGet]
        public async Task<IActionResult> GetProducts(string category = null, int page = 1, int pageSize = 10, decimal? minPrice = null, decimal? maxPrice = null)
        {
            // Would you mind wrapping this block in a try/catch? That way we can properly tell any clients and log if something went wrong.
            // If this is how things already exist, let's keep lines 32-59 the same, but if GetAllProductsAsync() was something that you made, would you mind making a generalized service
            // method called GetProducts() that we can pass our QueryObject to? This will help us reduce the complexity of filtering logic, because we can tell the query to filter!
            var allProducts = await _productService.GetAllProductsAsync();

            // Logic looks awesome, but filtering with SQL should be more performant!
            // Filter products
            var filteredProducts = new List<Product>();
            foreach (var product in allProducts)
            {
                bool includeProduct = true;

                if (!string.IsNullOrEmpty(category) && product.Category != category)
                    includeProduct = false;

                if (minPrice.HasValue && product.Price < minPrice.Value)
                    includeProduct = false;

                if (maxPrice.HasValue && product.Price > maxPrice.Value)
                    includeProduct = false;

                if (includeProduct)
                    filteredProducts.Add(product);
            }

            // Logic looks great, but we can use OFFSET/FETCH in our SQL! It should be more performant!
            // Apply pagination
            var pagedProducts = filteredProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Returning a defined object here might be the best thing to do, since the return type is staying the same!
            return Ok(new
            {
                Products = pagedProducts,
                TotalCount = filteredProducts.Count,
                Page = page,
                PageSize = pageSize
            });
        }

        // This looks like it might need the same update as lines 24-25!
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateRequest request)
        {
            // Love the 400 error handling!
            // Manual validation instead of using data annotations
            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest("Product name is required");
            }

            if (request.Name.Length < 3)
            {
                return BadRequest("Product name must be at least 3 characters");
            }

            if (request.Price <= 0)
            {
                return BadRequest("Product price must be greater than 0");
            }

            if (string.IsNullOrEmpty(request.Category))
            {
                return BadRequest("Product category is required");
            }

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Category = request.Category,
                InStock = request.InStock
            };

            // Would you mind wrapping this service and the return in a try/catch? That way we can tell any clients and log if something went wrong.
            var createdProduct = _productService.CreateProductAsync(product);

            return Ok(createdProduct);
        }

        // This is great, but since it's operating as filter for _productService.GetAllProductsAsync(), I believe we can use the
        // new generalized GetProducts() mentioned in line 38 with the category as a query param
        [HttpGet("category/{categoryName}")]
        public async Task<IActionResult> GetProductsByCategory(string categoryName)
        {
            var allProducts = await _productService.GetAllProductsAsync();

            var categoryProducts = new List<Product>();
            for (int i = 0; i < allProducts.Count; i++)
            {
                if (allProducts[i].Category.ToLower() == categoryName.ToLower())
                {
                    categoryProducts.Add(allProducts[i]);
                }
            }

            return Ok(categoryProducts);
        }

        // This is prettys similar to the comment mentioned on lines 112-113! We should be able to make the serach term a query param
        // and use a SQL "LIKE '%Name%' OR '%Description%'". This should be more performant!
        [HttpGet("search")]
        public IActionResult SearchProducts(string searchTerm)
        {
            var allProducts = _productService.GetAllProductsAsync();

            var matchingProducts = new List<Product>();
            foreach (var product in allProducts.Result)
            {
                if (product.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    product.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    matchingProducts.Add(product);
                }
            }

            return Ok(matchingProducts);
        }
    }

    // Would you mind making the strings nullable?
    public class ProductCreateRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public bool InStock { get; set; }
    }

    // I might be missing something, but if we're only using this class for returning data from the database
    // do you mind changing all instances of set; to init;? Since our database is our source of truth, we want these
    // properties to be immutable! We should also add the required modifier to the properties that are required
    // and if a string property is not required, we should default it with an empty string! 
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public bool InStock { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public interface IProductService
    {
        Task<List<Product>> GetAllProductsAsync();
        Task<Product> CreateProductAsync(Product product);
    }
}
