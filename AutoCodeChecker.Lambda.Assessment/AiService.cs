using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoCodeChecker.Lambda.Assessment;

public class AiService
{
    private readonly HttpClient _httpClient = new HttpClient();

    public async Task<string> GetFeedback(string studentCode, string taskDescription)
    {
        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey) || apiKey == "ВАШ_КЛЮЧ_ТУТ")
            return "ШІ Аналіз недоступний: Ключ API не налаштовано в конфігурації.";

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new {
                    role = "system",
                    content = "Ти — експерт з якості ПЗ (ISO 25010). Проаналізуй C# код студента. " +
                    "Оціни: 1. Чистоту (Clean Code), 2. Ефективність алгоритму, 3. Потенційні баги. " +
                    "Напиши відповідь українською мовою, дуже стисло (макс 3 речення). " +
                    "Якщо код ідеальний — похвали."
                },
                new {
                    role = "user",
                    content = $"Завдання: {taskDescription}\nКод студента:\n{studentCode}"
                }
            },
            max_tokens = 200
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Використовуємо локальну змінну apiKey
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"Помилка ШІ: {response.StatusCode}. Перевірте баланс або валідність ключа.";

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