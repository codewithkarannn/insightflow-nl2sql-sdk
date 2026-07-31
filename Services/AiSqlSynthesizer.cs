using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InsightFlow.Nl2Sql.Abstractions;
using InsightFlow.Nl2Sql.Models;
using Microsoft.Extensions.Options;

namespace InsightFlow.Nl2Sql.Services;

public class AiSqlSynthesizer : ISqlSynthesizer
{
    private readonly HttpClient _httpClient;
    private readonly Nl2SqlOptions _options;

    public AiSqlSynthesizer(HttpClient httpClient, IOptions<Nl2SqlOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

   public async Task<string> SynthesizeSqlAsync(
        string userPrompt, 
        DatabaseSchema schema, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) && !_options.BaseUrl.Contains("localhost"))
        {
            throw new InvalidOperationException("API Key is missing in Nl2SqlOptions.");
        }

        var formattedSchema = FormatSchemaToText(schema);

        var systemPrompt = $"""
            You are a strict, expert text-to-SQL engine.
            Your job is to translate user questions into valid SQL queries based strictly on the provided schema.

            ### TARGET DATABASE SCHEMA
            {formattedSchema}

            ### CRITICAL RULES
            1. Return ONLY the raw SQL query. Do NOT use markdown code fences (like ```sql).
            2. Do NOT add any explanations, introductory text, or concluding notes.
            3. Generate ONLY read-only SELECT queries.
            4. If the user asks to modify, update, delete, drop, or alter data/tables, return EXACTLY:
               SELECT 'ERROR: Destructive operations are strictly prohibited' AS Error;
            5. If the question cannot be answered using the schema, return EXACTLY:
               SELECT 'ERROR: Question cannot be answered with available schema' AS Error;
            """;

        var requestBody = new
        {
            model = _options.ModelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.0
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);

        // Dynamically append /chat/completions to whatever base URL is configured
        var baseUrlTrimmed = _options.BaseUrl.TrimEnd('/');
        var fullEndpoint = new Uri($"{baseUrlTrimmed}/chat/completions");

        using var request = new HttpRequestMessage(HttpMethod.Post, fullEndpoint);
        
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
        
        // OpenRouter optional tracking headers (ignored by OpenAI/Ollama)
        request.Headers.TryAddWithoutValidation("HTTP-Referer", "[https://github.com/InsightFlow](https://github.com/InsightFlow)");
        request.Headers.TryAddWithoutValidation("X-Title", "InsightFlow NL2SQL");

        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"LLM Provider API Error ({response.StatusCode}): {responseContent}");
        }

        using var doc = JsonDocument.Parse(responseContent);
        var rawSql = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return rawSql?.Trim() ?? string.Empty;
    }

    private static string FormatSchemaToText(DatabaseSchema schema)
    {
        var sb = new StringBuilder();

        foreach (var table in schema.Tables)
        {
            sb.AppendLine($"Table: {table.Name}");
            sb.AppendLine("Columns:");
            foreach (var col in table.Columns)
            {
                var pkFlag = col.IsPrimaryKey ? " [PK]" : "";
                var nullFlag = col.IsNullable ? "" : " NOT NULL";
                sb.AppendLine($"  - {col.Name} ({col.DataType}){pkFlag}{nullFlag}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

   
}