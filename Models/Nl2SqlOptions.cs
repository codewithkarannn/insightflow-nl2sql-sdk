namespace InsightFlow.Nl2Sql.Models;

public class Nl2SqlOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public int MaxRowLimit { get; set; } = 100;
    public int QueryTimeoutSeconds { get; set; } = 5;
}
