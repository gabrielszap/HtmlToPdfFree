using HtmlToPdfFree;
using HtmlToPdfFree.Contracts;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using Razor.Templating.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<InvoiceFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("invoice-report", async (InvoiceFactory invoiceFactory) =>
{
    Invoice invoice = invoiceFactory.Create();

    var html = await RazorTemplateEngine.RenderAsync("Views/InvoiceReport.cshtml", invoice);

    var browserFetcher = new BrowserFetcher();

    var installedBrowser = await browserFetcher.DownloadAsync();

    using var browser = await Puppeteer.LaunchAsync(new LaunchOptions 
    { 
        Headless = true,
        ExecutablePath = installedBrowser.GetExecutablePath(),
        Args = new[]
        {
            "--no-sandbox",
            "--disable-setuid-sandbox"
        }
    });

    using var page = await browser.NewPageAsync();

    await page.SetContentAsync(html);
    var pdfBytes = await page.PdfDataAsync(new PdfOptions
    {
        Format = PaperFormat.A4,
        PrintBackground = true
    });

    return Results.File(pdfBytes, "application/pdf", $"invoice-{invoice.Number}.pdf");
}).WithOpenApi();

app.UseHttpsRedirection();

app.Run();
