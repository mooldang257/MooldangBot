using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace MooldangBot.Application.Controllers.Auth
{
    /// <summary>
    /// [오시리스의 거울]: 외부 이미지 및 자원을 우회하여 전달하는 컨트롤러입니다.
    /// (Aegis of Proxy): 치지직 이미지의 Referer 체크 등을 우회하기 위해 사용됩니다.
    /// </summary>
    [ApiController]
    [Route("api/proxy")]
    public class ProxyController(IHttpClientFactory httpClientFactory) : ControllerBase
    {
        [HttpGet("image")]
        public async Task<IActionResult> ProxyImage([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url)) return NotFound();

            if (url.Contains("pstatic.net"))
            {
                if (url.Contains("type="))
                    url = System.Text.RegularExpressions.Regex.Replace(url, "type=[^&]+", "type=f120_120");
                else
                    url += (url.Contains("?") ? "&" : "?") + "type=f120_120";
            }
        
            try 
            {
                var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Referer", "https://chzzk.naver.com/");
                
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return NotFound();
                
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
                var stream = await response.Content.ReadAsStreamAsync();
                
                return File(stream, contentType);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }
    }
}
