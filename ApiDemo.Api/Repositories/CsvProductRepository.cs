using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using ApiDemo.Api.Models;
using ApiDemo.Api.Options;
using Microsoft.Extensions.Options;

namespace ApiDemo.Api.Repositories;

public class CsvProductRepository : IProductRepository
{
    private const string Header = "Id,Name,Description,Price";

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CsvProductRepository(IHostEnvironment environment, IOptions<CsvStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);

        var configuredPath = options.Value.ProductsFile ?? "Data/products.csv";
        _filePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);

        EnsureFileInitialized();
    }

    public async Task<IReadOnlyCollection<Product>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var products = await LoadProductsAsync();
            return products
                .OrderBy(p => p.Id)
                .Select(Clone)
                .ToArray();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            var products = await LoadProductsAsync();
            var match = products.FirstOrDefault(p => p.Id == id);
            return match is null ? null : Clone(match);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Product> CreateAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        await _lock.WaitAsync();
        try
        {
            var products = await LoadProductsAsync();
            var nextId = products.Count == 0 ? 1 : products.Max(p => p.Id) + 1;

            var newProduct = new Product
            {
                Id = nextId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price
            };

            products.Add(newProduct);
            await SaveProductsAsync(products);

            return Clone(newProduct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        await _lock.WaitAsync();
        try
        {
            var products = await LoadProductsAsync();
            var index = products.FindIndex(p => p.Id == product.Id);
            if (index < 0)
            {
                return false;
            }

            products[index] = new Product
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price
            };

            await SaveProductsAsync(products);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            var products = await LoadProductsAsync();
            var removed = products.RemoveAll(p => p.Id == id) > 0;
            if (!removed)
            {
                return false;
            }

            await SaveProductsAsync(products);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void EnsureFileInitialized()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_filePath))
        {
            using var stream = new FileStream(_filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.WriteLine(Header);
        }
    }

    private async Task<List<Product>> LoadProductsAsync()
    {
        var products = new List<Product>();

        if (!File.Exists(_filePath))
        {
            return products;
        }

        using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        var isFirstLine = true;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (isFirstLine)
            {
                isFirstLine = false;
                if (line.Trim().Equals(Header, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            if (fields.Length < 4)
            {
                continue;
            }

            if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            if (!decimal.TryParse(fields[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
            {
                continue;
            }

            products.Add(new Product
            {
                Id = id,
                Name = fields[1],
                Description = fields[2],
                Price = price
            });
        }

        return products;
    }

    private async Task SaveProductsAsync(IEnumerable<Product> products)
    {
        using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        await writer.WriteLineAsync(Header);

        foreach (var product in products.OrderBy(p => p.Id))
        {
            await writer.WriteLineAsync(ToCsvLine(product));
        }
    }

    private static Product Clone(Product source) =>
        new()
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Price = source.Price
        };

    // Basic CSV parsing that respects quoted fields.
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var current = line[i];
            if (inQuotes)
            {
                if (current == '"')
                {
                    var nextIsQuote = i + 1 < line.Length && line[i + 1] == '"';
                    if (nextIsQuote)
                    {
                        builder.Append('"');
                        i++; // skip escaped quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    builder.Append(current);
                }
            }
            else
            {
                switch (current)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        result.Add(builder.ToString());
                        builder.Clear();
                        break;
                    default:
                        builder.Append(current);
                        break;
                }
            }
        }

        result.Add(builder.ToString());
        return result.ToArray();
    }

    private static string ToCsvLine(Product product)
    {
        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var requiresQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (!requiresQuotes)
            {
                return value;
            }

            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return string.Join(",", new[]
        {
            product.Id.ToString(CultureInfo.InvariantCulture),
            Escape(product.Name),
            Escape(product.Description),
            product.Price.ToString(CultureInfo.InvariantCulture)
        });
    }
}
