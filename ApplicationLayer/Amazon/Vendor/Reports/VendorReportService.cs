using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO.Compression;
using System.Security.Cryptography;
using BusinessLayer.Amazon.Vendor.Reports.Models;

namespace ApplicationLayer.Amazon.Vendor.Reports;

/// <summary>
/// Servicio para gestionar reportes de Amazon Vendor
/// </summary>
public class VendorReportService
{
    private readonly HttpClient _httpClient;

    public VendorReportService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Genera un reporte de tráfico de Vendor en Amazon
    /// </summary>
    public async Task<GenerateReportResult> GenerateVendorTrafficReportAsync(
        GenerateVendorTrafficReportRequest request,
        string accessToken,
        string accessKey,
        string secretKey,
        string awsRegion,
        string serviceName,
        string reportsUrl)
    {
        try
        {
            // Preparar el body del request
            var requestBody = new
            {
                reportType = request.ReportType,
                marketplaceIds = request.MarketplaceIds,
                dataStartTime = request.DataStartTime,
                dataEndTime = request.DataEndTime,
                reportOptions = new
                {
                    reportPeriod = request.ReportOptions?.ReportPeriod ?? "WEEK"
                }
            };

            var jsonBody = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Crear el request HTTP
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, reportsUrl)
            {
                Content = content
            };

            // Agregar headers requeridos
            httpRequest.Headers.Add("x-amz-access-token", accessToken);
            httpRequest.Headers.Add("Accept", "application/json");

            // Parsear la URL para obtener host y path
            var uri = new Uri(reportsUrl);
            var host = uri.Host;
            var path = uri.AbsolutePath;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            // Generar firma AWS Signature V4
            var signedHeaders = GenerateAwsSignature(
                httpRequest: httpRequest,
                accessKey: accessKey,
                secretKey: secretKey,
                awsRegion: awsRegion,
                serviceName: serviceName,
                host: host,
                path: path,
                timestamp: timestamp,
                dateStamp: dateStamp,
                requestBody: jsonBody,
                accessToken: accessToken);

            // Agregar headers de autenticación AWS
            foreach (var header in signedHeaders)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Hacer la petición a Amazon
            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                
                return new GenerateReportResult
                {
                    Success = false,
                    Message = $"Amazon respondió con error. Status: {response.StatusCode}. Detalle: {errorContent}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var reportResponse = JsonSerializer.Deserialize<AmazonReportResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (reportResponse == null || string.IsNullOrEmpty(reportResponse.ReportId))
            {
                return new GenerateReportResult
                {
                    Success = false,
                    Message = "Amazon respondió correctamente pero no devolvió un Report ID válido"
                };
            }

            return new GenerateReportResult
            {
                Success = true,
                Message = "Reporte de tráfico generado exitosamente",
                ReportId = reportResponse.ReportId
            };
        }
        catch (HttpRequestException ex)
        {
            return new GenerateReportResult
            {
                Success = false,
                Message = $"Error de conexión con Amazon: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            return new GenerateReportResult
            {
                Success = false,
                Message = $"La petición a Amazon excedió el tiempo de espera: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new GenerateReportResult
            {
                Success = false,
                Message = $"Error inesperado al generar el reporte: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Genera un reporte de inventario de Vendor en Amazon
    /// </summary>
    public async Task<GenerateReportResult> GenerateVendorInventoryReportAsync(
        GenerateVendorInventoryReportRequest request,
        string accessToken,
        string accessKey,
        string secretKey,
        string awsRegion,
        string serviceName,
        string reportsUrl)
    {
        try
        {
            // Preparar el body del request
            var requestBody = new
            {
                reportType = request.ReportType,
                marketplaceIds = request.MarketplaceIds,
                dataStartTime = request.DataStartTime,
                dataEndTime = request.DataEndTime,
                reportOptions = new
                {
                    reportPeriod = request.ReportOptions?.ReportPeriod ?? "WEEK",
                    sellingProgram = request.ReportOptions?.SellingProgram ?? "RETAIL",
                    distributorView = request.ReportOptions?.DistributorView ?? "MANUFACTURING"
                }
            };

            var jsonBody = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Crear el request HTTP
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, reportsUrl)
            {
                Content = content
            };

            // Agregar headers requeridos
            httpRequest.Headers.Add("x-amz-access-token", accessToken);
            httpRequest.Headers.Add("Accept", "application/json");

            // Parsear la URL para obtener host y path
            var uri = new Uri(reportsUrl);
            var host = uri.Host;
            var path = uri.AbsolutePath;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            // Generar firma AWS Signature V4
            var signedHeaders = GenerateAwsSignature(
                httpRequest: httpRequest,
                accessKey: accessKey,
                secretKey: secretKey,
                awsRegion: awsRegion,
                serviceName: serviceName,
                host: host,
                path: path,
                timestamp: timestamp,
                dateStamp: dateStamp,
                requestBody: jsonBody,
                accessToken: accessToken);

            // Agregar headers de autenticación AWS
            foreach (var header in signedHeaders)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Hacer la petición a Amazon
            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                
                return new GenerateReportResult
                {
                    Success = false,
                    Message = $"Amazon respondió con error. Status: {response.StatusCode}. Detalle: {errorContent}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var reportResponse = JsonSerializer.Deserialize<AmazonReportResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (reportResponse == null || string.IsNullOrEmpty(reportResponse.ReportId))
            {
                return new GenerateReportResult
                {
                    Success = false,
                    Message = "Amazon respondió correctamente pero no devolvió un Report ID válido"
                };
            }

            return new GenerateReportResult
            {
                Success = true,
                Message = "Reporte de inventario generado exitosamente",
                ReportId = reportResponse.ReportId
            };
        }
        catch (HttpRequestException ex)
        {
            return new GenerateReportResult
            {
                Success = false,
                Message = $"Error de conexión con Amazon: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            return new GenerateReportResult
            {
                Success = false,
                Message = $"La petición a Amazon excedió el tiempo de espera: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new GenerateReportResult
            {
                Success = false,
                Message = $"Error inesperado al generar el reporte: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Genera la firma AWS Signature V4 para autenticar el request
    /// </summary>
    private Dictionary<string, string> GenerateAwsSignature(
        HttpRequestMessage httpRequest,
        string accessKey,
        string secretKey,
        string awsRegion,
        string serviceName,
        string host,
        string path,
        string timestamp,
        string dateStamp,
        string requestBody,
        string accessToken)
    {
        var algorithm = "AWS4-HMAC-SHA256";
        var credentialScope = $"{dateStamp}/{awsRegion}/{serviceName}/aws4_request";

        // Paso 1: Crear canonical request
        // IMPORTANTE: Los headers deben estar en orden alfabético
        // host -> x-amz-access-token -> x-amz-date
        var canonicalHeaders = $"host:{host}\nx-amz-access-token:{accessToken}\nx-amz-date:{timestamp}\n";
        var signedHeaders = "host;x-amz-access-token;x-amz-date";
        
        var payloadHash = ComputeSha256Hash(requestBody);
        
        var canonicalRequest = $"POST\n{path}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        // Paso 2: Crear string to sign
        var canonicalRequestHash = ComputeSha256Hash(canonicalRequest);
        var stringToSign = $"{algorithm}\n{timestamp}\n{credentialScope}\n{canonicalRequestHash}";

        // Paso 3: Calcular signature
        var signingKey = GetSignatureKey(secretKey, dateStamp, awsRegion, serviceName);
        var signature = BitConverter.ToString(HmacSha256(signingKey, stringToSign)).Replace("-", "").ToLower();

        // Paso 4: Crear authorization header
        var authorizationHeader = $"{algorithm} Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        return new Dictionary<string, string>
        {
            { "Authorization", authorizationHeader },
            { "x-amz-date", timestamp }
        };
    }

    private string ComputeSha256Hash(string rawData)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }

    private byte[] HmacSha256(byte[] key, string data)
    {
        using (var hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }
    }

    private byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        var kSecret = Encoding.UTF8.GetBytes($"AWS4{key}");
        var kDate = HmacSha256(kSecret, dateStamp);
        var kRegion = HmacSha256(kDate, regionName);
        var kService = HmacSha256(kRegion, serviceName);
        var kSigning = HmacSha256(kService, "aws4_request");
        return kSigning;
    }

    /// <summary>
    /// Consulta el estado de un reporte en Amazon
    /// </summary>
    public async Task<GetReportStatusResult> GetReportStatusAsync(
        string reportId,
        string accessToken,
        string accessKey,
        string secretKey,
        string awsRegion,
        string serviceName,
        string reportsBaseUrl)
    {
        try
        {
            // Construir la URL con el reportId
            var reportUrl = $"{reportsBaseUrl.TrimEnd('/')}/{reportId}";

            // Crear el request HTTP
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, reportUrl);

            // Agregar headers requeridos
            httpRequest.Headers.Add("x-amz-access-token", accessToken);
            httpRequest.Headers.Add("Accept", "application/json");

            // Parsear la URL para obtener host y path
            var uri = new Uri(reportUrl);
            var host = uri.Host;
            var path = uri.AbsolutePath;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            // Generar firma AWS Signature V4 para GET (sin body)
            var signedHeaders = GenerateAwsSignatureForGet(
                httpRequest: httpRequest,
                accessKey: accessKey,
                secretKey: secretKey,
                awsRegion: awsRegion,
                serviceName: serviceName,
                host: host,
                path: path,
                timestamp: timestamp,
                dateStamp: dateStamp,
                accessToken: accessToken);

            // Agregar headers de autenticación AWS
            foreach (var header in signedHeaders)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Hacer la petición a Amazon
            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new GetReportStatusResult
                    {
                        Success = false,
                        Message = $"El reporte con ID '{reportId}' no fue encontrado en Amazon. Verifica que el Report ID sea correcto."
                    };
                }

                return new GetReportStatusResult
                {
                    Success = false,
                    Message = $"Amazon respondió con error. Status: {response.StatusCode}. Detalle: {errorContent}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var reportStatus = JsonSerializer.Deserialize<ReportStatusResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (reportStatus == null)
            {
                return new GetReportStatusResult
                {
                    Success = false,
                    Message = "Amazon respondió correctamente pero el contenido de la respuesta no es válido"
                };
            }

            // Mensajes específicos según el estado
            var statusMessage = reportStatus.ProcessingStatus switch
            {
                "DONE" => $"Reporte completado exitosamente. Document ID: {reportStatus.ReportDocumentId}",
                "IN_PROGRESS" => "El reporte está siendo procesado por Amazon. Intenta nuevamente en unos momentos.",
                "IN_QUEUE" => "El reporte está en cola esperando ser procesado.",
                "CANCELLED" => "El reporte fue cancelado.",
                "FATAL" => "Ocurrió un error fatal al procesar el reporte.",
                _ => $"Estado del reporte: {reportStatus.ProcessingStatus}"
            };

            return new GetReportStatusResult
            {
                Success = true,
                Message = statusMessage,
                ReportStatus = reportStatus
            };
        }
        catch (HttpRequestException ex)
        {
            return new GetReportStatusResult
            {
                Success = false,
                Message = $"Error de conexión con Amazon: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            return new GetReportStatusResult
            {
                Success = false,
                Message = $"La petición a Amazon excedió el tiempo de espera: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new GetReportStatusResult
            {
                Success = false,
                Message = $"Error inesperado al consultar el estado del reporte: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Genera la firma AWS Signature V4 para peticiones GET (sin body)
    /// </summary>
    private Dictionary<string, string> GenerateAwsSignatureForGet(
        HttpRequestMessage httpRequest,
        string accessKey,
        string secretKey,
        string awsRegion,
        string serviceName,
        string host,
        string path,
        string timestamp,
        string dateStamp,
        string accessToken)
    {
        var algorithm = "AWS4-HMAC-SHA256";
        var credentialScope = $"{dateStamp}/{awsRegion}/{serviceName}/aws4_request";

        // Paso 1: Crear canonical request (para GET, el payload es una cadena vacía)
        // IMPORTANTE: Los headers deben estar en orden alfabético
        // host -> x-amz-access-token -> x-amz-date
        var canonicalHeaders = $"host:{host}\nx-amz-access-token:{accessToken}\nx-amz-date:{timestamp}\n";
        var signedHeaders = "host;x-amz-access-token;x-amz-date";
        
        var payloadHash = ComputeSha256Hash(""); // Empty payload for GET
        
        var canonicalRequest = $"GET\n{path}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        // Paso 2: Crear string to sign
        var canonicalRequestHash = ComputeSha256Hash(canonicalRequest);
        var stringToSign = $"{algorithm}\n{timestamp}\n{credentialScope}\n{canonicalRequestHash}";

        // Paso 3: Calcular signature
        var signingKey = GetSignatureKey(secretKey, dateStamp, awsRegion, serviceName);
        var signature = BitConverter.ToString(HmacSha256(signingKey, stringToSign)).Replace("-", "").ToLower();

        // Paso 4: Crear authorization header
        var authorizationHeader = $"{algorithm} Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        return new Dictionary<string, string>
        {
            { "Authorization", authorizationHeader },
            { "x-amz-date", timestamp }
        };
    }

    /// <summary>
    /// Paso 3a: Obtiene el documento del reporte (pre-signed URL y algoritmo de compresión)
    /// URL: GET {baseUrl}/documents/{reportDocumentId}
    /// </summary>
    public async Task<DownloadReportResult> GetAndDownloadReportDocumentAsync(
        string reportDocumentId,
        string accessToken,
        string accessKey,
        string secretKey,
        string awsRegion,
        string serviceName,
        string reportsBaseUrl)
    {
        try
        {
            // Paso 3a: Llamar a Amazon para obtener la pre-signed URL del documento
            var documentUrl = $"{reportsBaseUrl.TrimEnd('/')}/documents/{reportDocumentId}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, documentUrl);
            httpRequest.Headers.Add("x-amz-access-token", accessToken);
            httpRequest.Headers.Add("Accept", "application/json");

            var uri = new Uri(documentUrl);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            var awsHeaders = GenerateAwsSignatureForGet(
                httpRequest: httpRequest,
                accessKey: accessKey,
                secretKey: secretKey,
                awsRegion: awsRegion,
                serviceName: serviceName,
                host: uri.Host,
                path: uri.AbsolutePath,
                timestamp: timestamp,
                dateStamp: dateStamp,
                accessToken: accessToken);

            foreach (var header in awsHeaders)
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);

            var documentResponse = await _httpClient.SendAsync(httpRequest);

            if (!documentResponse.IsSuccessStatusCode)
            {
                var errorContent = await documentResponse.Content.ReadAsStringAsync();

                if (documentResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new DownloadReportResult
                    {
                        Success = false,
                        Message = $"El documento de reporte '{reportDocumentId}' no fue encontrado en Amazon. " +
                                  "Verifica que el reportDocumentId sea correcto y que el reporte tenga estado DONE."
                    };
                }

                return new DownloadReportResult
                {
                    Success = false,
                    Message = $"Amazon respondió con error al obtener el documento. Status: {documentResponse.StatusCode}. Detalle: {errorContent}"
                };
            }

            var documentContent = await documentResponse.Content.ReadAsStringAsync();
            var documentInfo = JsonSerializer.Deserialize<ReportDocumentResponse>(documentContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (documentInfo == null || string.IsNullOrEmpty(documentInfo.Url))
            {
                return new DownloadReportResult
                {
                    Success = false,
                    Message = "Amazon no devolvió una URL válida para descargar el documento del reporte."
                };
            }

            // Paso 3b: Descargar el archivo desde la pre-signed URL de S3
            return await DownloadAndDecompressAsync(documentInfo.Url, documentInfo.CompressionAlgorithm, reportDocumentId);
        }
        catch (HttpRequestException ex)
        {
            return new DownloadReportResult
            {
                Success = false,
                Message = $"Error de conexión con Amazon al obtener el documento: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            return new DownloadReportResult
            {
                Success = false,
                Message = $"La petición a Amazon excedió el tiempo de espera: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new DownloadReportResult
            {
                Success = false,
                Message = $"Error inesperado al obtener el documento del reporte: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Paso 3b: Descarga el archivo desde la pre-signed URL de S3 y descomprime el GZIP
    /// Amazon entrega el contenido como GZIP. Dentro hay un JSON que es el reporte real.
    /// </summary>
    private async Task<DownloadReportResult> DownloadAndDecompressAsync(
        string presignedUrl,
        string compressionAlgorithm,
        string reportDocumentId)
    {
        try
        {
            // Descargar el archivo desde S3 (la pre-signed URL ya tiene autenticación embebida)
            using var s3Client = new HttpClient();
            s3Client.Timeout = TimeSpan.FromSeconds(120);

            var s3Response = await s3Client.GetAsync(presignedUrl);

            if (!s3Response.IsSuccessStatusCode)
            {
                var errorContent = await s3Response.Content.ReadAsStringAsync();
                return new DownloadReportResult
                {
                    Success = false,
                    Message = $"Error al descargar el archivo del reporte desde S3. Status: {s3Response.StatusCode}. " +
                              "La URL de descarga puede haber expirado. Obtén un nuevo reportDocumentId."
                };
            }

            var fileBytes = await s3Response.Content.ReadAsByteArrayAsync();

            if (fileBytes.Length == 0)
            {
                return new DownloadReportResult
                {
                    Success = false,
                    Message = "El archivo descargado está vacío. El reporte puede no contener datos para el período solicitado."
                };
            }

            // Descomprimir según el algoritmo indicado por Amazon
            string jsonContent;

            if (string.Equals(compressionAlgorithm, "GZIP", StringComparison.OrdinalIgnoreCase))
            {
                jsonContent = await DecompressGzipAsync(fileBytes);
            }
            else if (string.IsNullOrEmpty(compressionAlgorithm))
            {
                // Sin compresión, el contenido ya es texto plano
                jsonContent = Encoding.UTF8.GetString(fileBytes);
            }
            else
            {
                return new DownloadReportResult
                {
                    Success = false,
                    Message = $"Algoritmo de compresión no soportado: '{compressionAlgorithm}'. Solo se soporta GZIP."
                };
            }

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return new DownloadReportResult
                {
                    Success = false,
                    Message = "El contenido del reporte está vacío después de descomprimir. " +
                              "Es posible que no haya datos para el período y marketplace solicitados."
                };
            }

            // Parsear el JSON para devolverlo como objeto estructurado
            var reportJson = JsonNode.Parse(jsonContent);

            if (reportJson == null)
            {
                return new DownloadReportResult
                {
                    Success = false,
                    Message = "El contenido del reporte no es un JSON válido."
                };
            }

            return new DownloadReportResult
            {
                Success = true,
                Message = $"Reporte descargado y descomprimido exitosamente. Documento ID: {reportDocumentId}",
                ReportContent = reportJson,
                CompressionAlgorithm = compressionAlgorithm
            };
        }
        catch (InvalidDataException ex)
        {
            return new DownloadReportResult
            {
                Success = false,
                Message = $"Error al descomprimir el archivo GZIP del reporte. El archivo puede estar corrupto o en un formato inesperado. Detalle: {ex.Message}"
            };
        }
        catch (JsonException ex)
        {
            return new DownloadReportResult
            {
                Success = false,
                Message = $"El contenido del reporte no tiene formato JSON válido. Detalle: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new DownloadReportResult
            {
                Success = false,
                Message = $"Error inesperado al descargar o descomprimir el reporte: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Descomprime un arreglo de bytes GZIP y retorna el contenido como string UTF-8
    /// </summary>
    private async Task<string> DecompressGzipAsync(byte[] compressedBytes)
    {
        using var compressedStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();

        await gzipStream.CopyToAsync(decompressedStream);
        return Encoding.UTF8.GetString(decompressedStream.ToArray());
    }
}
