namespace InsightFlow.Nl2Sql.Models;

public record Nl2SqlQueryResult(
    bool IsSuccess,
    string GeneratedSql,
    List<Dictionary<string, object?>>? Data,
    string? ErrorMessage
);
