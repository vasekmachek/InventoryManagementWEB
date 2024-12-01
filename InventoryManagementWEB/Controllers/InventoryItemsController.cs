using InventoryManagementWEB.Data;
using InventoryManagementWEB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using OfficeOpenXml;
using System.IO;

public class InventoryItemsController : Controller
{
    private readonly ApplicationDbContext _context;

    public InventoryItemsController(ApplicationDbContext context)
    {
        _context = context;
    }
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryItem inventoryItem)
    {
        if(ModelState.IsValid)
        {   
            var existingItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Name == inventoryItem.Name);

            if (existingItem != null)
            {
                ModelState.AddModelError("Name", "Položka se stejným názvem již existuje.");
                return View(existingItem);
            }


            _context.InventoryItems.Add(inventoryItem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(inventoryItem);
    }
    public async Task<IActionResult> Edit(int id)
    {
        var inventoryItem = await _context.InventoryItems.FindAsync(id);
        if (inventoryItem == null)
        {
            return NotFound();
        }
        return View(inventoryItem);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InventoryItem inventoryItem)
    {
        if (id != inventoryItem.Id)
        {
            return NotFound();
        }
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(inventoryItem);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventoryItemExists(inventoryItem.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
                
            }
            return RedirectToAction(nameof(Index));
        }
        return View(inventoryItem);
    }
    private bool InventoryItemExists(int id)
    {
        return _context.InventoryItems.Any(e => e.Id == id);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var inventoryItem = await _context.InventoryItems
             .FirstOrDefaultAsync(m => m.Id == id);
        if (inventoryItem == null)
        {
            return NotFound();
        }
        return View(inventoryItem);
    }
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var inventoryItem = await _context.InventoryItems.FindAsync(id);
        if (inventoryItem == null)
        {
            return NotFound();
        }
        _context.InventoryItems.Remove(inventoryItem);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> DeleteAll()
    {
        var items = await _context.InventoryItems.ToListAsync();

        if (items.Any())
        {
            _context.InventoryItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    public IActionResult ExportToExcel()
    {
        var items = _context.InventoryItems.ToList();
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Inventory Items");
            worksheet.Cells[1, 1].Value = "Id";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Quantity";
            worksheet.Cells[1, 4].Value = "Price";

            for (int i = 0; i < items.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = items[i].Id;
                worksheet.Cells[i + 2, 2].Value = items[i].Name;
                worksheet.Cells[i + 2, 3].Value = items[i].Quantity;
                worksheet.Cells[i + 2, 4].Value = items[i].Price;

            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            var fileName = $"InventoryItems_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
	
    public IActionResult Import()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> ImportData(IFormFile file)
    {
        
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Prosím nahrajte platný Excel (XLSX) soubor.");
            return View();
        }
        using(var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using(var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    var name = worksheet.Cells[row, 1].Value?.ToString().Trim();
                    var quantity = int.TryParse(worksheet.Cells[row, 2].Value?.ToString().Trim(), out var parsedQuantity) ? parsedQuantity : 0;
					var price = int.TryParse(worksheet.Cells[row, 2].Value?.ToString().Trim(), out var parsedPrice) ? parsedPrice : 0;

                    var item = new InventoryItem
                    {
                        Name = name,
                        Quantity = quantity,
                        Price = price,
                    };
                    _context.InventoryItems.Add(item);
				}
                await _context.SaveChangesAsync();
            }
        }
        return RedirectToAction("Index");
    }


    public async Task<IActionResult> Index(string Name, int? QuantityMin, int? QuantityMax, decimal? PriceMin, decimal? PriceMax, string sortOrder)
    {
        var items = _context.InventoryItems.AsQueryable();

        if (!string.IsNullOrEmpty(Name))
        {
            items = items.Where(i => i.Name.Contains(Name));
        }
        if (QuantityMin.HasValue)
        {
            items = items.Where(i => i.Quantity >= QuantityMin.Value);
        }
        if (QuantityMax.HasValue)
        {
            items = items.Where(i => i.Quantity <= QuantityMax.Value);  // Oprava podmínky na <=
        }
        if (PriceMin.HasValue)
        {
            items = items.Where(i => i.Price >= PriceMin.Value);
        }
        if (PriceMax.HasValue)
        {
            items = items.Where(i => i.Price <= PriceMax.Value);  // Oprava podmínky na <=
        }
        //Sorting
        items = sortOrder switch
        {
            "name_desc" => items.OrderByDescending(i => i.Name),
            "quantity_asc" => items.OrderBy(i => i.Quantity),
            "quantity_desc" => items.OrderByDescending(i => i.Quantity),
            "price_asc" => items.OrderBy(i => (double)i.Price),
            "price_desc" => items.OrderByDescending(i => (double)i.Price),
            _ => items.OrderBy(i => i.Name)
        };
        var resultList = await items.ToListAsync(); // Přidání .ToListAsync()

        return View(resultList); // Ujisti se, že vracíš správný model
    }

}
