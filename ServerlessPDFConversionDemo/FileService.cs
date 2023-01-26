using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Mirecad.Toolbox.Http;

namespace ServerlessPDFConversionDemo
{
    public class FileService : IDisposable
    {
        private readonly HttpClient _httpClient;

        public FileService(IOptions<AuthenticationOptions> options)
        {
            var handler = new AzureAdAppMessageHandler(options.Value.Resource);
            _httpClient = new HttpClient(handler);
        }

        public async Task<string> UploadStreamAsync(string path, Stream content, string contentType)
        {
            var tmpFileName = $"{Guid.NewGuid()}.{MimeTypes.MimeTypeMap.GetExtension(contentType)}";
            var requestUrl = $"{path}root:/{tmpFileName}:/content";
            var requestContent = new StreamContent(content);
            requestContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            var response = await _httpClient.PutAsync(requestUrl, requestContent);
            if (response.IsSuccessStatusCode)
            {
                dynamic file = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                return file?.id;
            }

            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"Upload file failed with status {response.StatusCode} and message {message}");
        }

        public async Task<byte[]> DownloadConvertedFileAsync(string path, string fileId, string targetFormat)
        {
            var requestUrl = $"{path}{fileId}/content?format={targetFormat}";
            var response = await _httpClient.GetAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                var fileContent = await response.Content.ReadAsByteArrayAsync();
                return fileContent;
            }

            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"Download of converted file failed with status {response.StatusCode} and message {message}");
        }

        public async Task DeleteFileAsync(string path, string fileId)
        {
            var requestUrl = $"{path}{fileId}";
            var response = await _httpClient.DeleteAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                throw new Exception($"Delete file failed with status {response.StatusCode} and message {message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
