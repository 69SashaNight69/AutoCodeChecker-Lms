using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoCodeChecker.Lambda.Assessment;

public class AiService
{
    private readonly string _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "ВАШ_КЛЮЧ_ТУТ";
    private readonly HttpClient _httpClient = new HttpClient();

    public async Task<string> GetFeedback(string studentCode, string taskDescription)
    {
        if (string.IsNullOrEmpty(_apiKey) || _apiKey == "ВАШ_КЛЮЧ_ТУТ")
            return "ШІ Аналіз недоступний: Ключ API не налаштовано.";

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = "Ти — викладач програмування. Проаналізуй код студента на C#. Напиши короткий відгук (2-3 речення) про його стиль, ефективність та чистоту. Якщо є помилки, вкажи на них." },
                new { role = "user", content = $"Завдання: {taskDescription}\nКод студента:\n{studentCode}" }
            },
            max_tokens = 200
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"Помилка ШІ: {response.StatusCode}";

            using var doc = JsonDocument.Parse(responseString);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "ШІ не надав відповіді.";
        }
        catch (Exception ex)
        {
            return $"Помилка зв'язку з ШІ: {ex.Message}";
        }
    }
}