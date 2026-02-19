using BusinessLayer.Shared;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http;
using System.Text;
using System.Text.Json;

namespace ApplicationLayer.Shared;

/// <summary>
/// Servicio para generar PDFs usando CraftMyPDF
/// </summary>
public class CraftMyPdfService
{
    private readonly CraftMyPdfSettings _settings;
    private readonly HttpClient _httpClient;

    public CraftMyPdfService(IOptions<CraftMyPdfSettings> settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient("CraftMyPdf");
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
    }

    /// <summary>
    /// Genera un PDF a partir de un template y devuelve el PDF como array de bytes.
    /// Serializa el objeto data en camelCase (para templates que usan camelCase, ej. contrato).
    /// </summary>
    /// <param name="templateId">ID del template en CraftMyPDF</param>
    /// <param name="data">Datos JSON para llenar el template</param>
    /// <returns>Array de bytes del PDF generado</returns>
    public async Task<byte[]> GeneratePdfAsync(string templateId, object data)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
        return await GeneratePdfInternalAsync(templateId, data, options);
    }

    /// <summary>
    /// Genera un PDF a partir de un template con opciones de serialización personalizadas para el data.
    /// Use esto para templates que esperan snake_case (ej. invoice_no, bill_to) aplicando PropertyNamingPolicy = null y [JsonPropertyName] en el DTO.
    /// </summary>
    public async Task<byte[]> GeneratePdfWithDataOptionsAsync(string templateId, object data, JsonSerializerOptions dataSerializationOptions)
    {
        return await GeneratePdfInternalAsync(templateId, data, dataSerializationOptions);
    }

    private async Task<byte[]> GeneratePdfInternalAsync(string templateId, object data, JsonSerializerOptions dataSerializationOptions)
    {
        try
        {
            // Serializar el objeto data con las opciones indicadas (camelCase o preservar [JsonPropertyName])
            var dataJson = JsonSerializer.Serialize(data, dataSerializationOptions);
            var dataObject = JsonSerializer.Deserialize<JsonElement>(dataJson);
            
            var generateRequest = new
            {
                template_id = templateId,
                data = dataObject,
                export_type = "json" // Necesario para obtener la URL del PDF en la respuesta
            };

            var jsonContent = JsonSerializer.Serialize(generateRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null, // Mantener el nombre exacto: template_id
                WriteIndented = false
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // CraftMyPDF usa el endpoint /v1/create para generar PDFs desde templates
            var generateResponse = await _httpClient.PostAsync("/v1/create", content);
            
            if (!generateResponse.IsSuccessStatusCode)
            {
                var errorContent = await generateResponse.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"CraftMyPDF API error: {generateResponse.StatusCode}. Details: {errorContent}");
            }

            var responseJson = await generateResponse.Content.ReadAsStringAsync();
            
            // CraftMyPDF devuelve un objeto con 'file' (URL del PDF) y 'file_name'
            var responseData = JsonSerializer.Deserialize<CraftMyPdfResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (responseData?.File == null || string.IsNullOrWhiteSpace(responseData.File))
            {
                throw new Exception($"CraftMyPDF no devolvió una URL de descarga válida. Respuesta: {responseJson}");
            }

            // Paso 2: Descargar el PDF desde la URL proporcionada
            var pdfBytes = await DownloadPdfAsync(responseData.File);

            return pdfBytes;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al comunicarse con CraftMyPDF: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new Exception($"Error al procesar la respuesta de CraftMyPDF: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error inesperado al generar el PDF: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Descarga el PDF desde la URL proporcionada por CraftMyPDF
    /// </summary>
    /// <param name="pdfUrl">URL del PDF</param>
    /// <returns>Array de bytes del PDF</returns>
    private async Task<byte[]> DownloadPdfAsync(string pdfUrl)
    {
        try
        {
            // Usar un HttpClient sin autenticación para descargar el archivo público
            using var downloadClient = new HttpClient();
            downloadClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

            var response = await downloadClient.GetAsync(pdfUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Error al descargar el PDF: {response.StatusCode}");
            }

            var pdfBytes = await response.Content.ReadAsByteArrayAsync();

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                throw new Exception("El PDF descargado está vacío");
            }

            return pdfBytes;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al descargar el PDF desde la URL: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Clase para deserializar la respuesta de CraftMyPDF
    /// </summary>
    private class CraftMyPdfResponse
    {
        public string? File { get; set; }
        public string? FileName { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
