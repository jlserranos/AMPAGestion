using Microsoft.JSInterop;

namespace AMPAGestion.Services;

public class ExportService
{
    private readonly IJSRuntime _js;

    public ExportService(IJSRuntime js) => _js = js;

    public async Task ExportarPDFAsync(string titulo, string[] columnas, object[][] filas)
        => await _js.InvokeVoidAsync("exportarPDF", titulo, columnas, filas);

    public async Task ExportarExcelAsync(string titulo, string[] columnas, object[][] filas)
        => await _js.InvokeVoidAsync("exportarExcel", titulo, columnas, filas);

    public async Task ImprimirAsync(string titulo, string[] columnas, object[][] filas)
        => await _js.InvokeVoidAsync("imprimirTabla", titulo, columnas, filas);
}
