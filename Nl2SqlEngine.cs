using InsightFlow.Nl2Sql.Abstractions;
using InsightFlow.Nl2Sql.Models;
using Microsoft.Extensions.Options;

namespace InsightFlow.Nl2Sql;

public class Nl2SqlEngine : INl2SqlEngine
{
    private readonly ISchemaExtractor _schemaExtractor;
    private readonly ISqlSynthesizer _synthesizer;
    private readonly ISqlGuardrail _guardrail;
    private readonly Nl2SqlOptions _options;
    private readonly ISqlExecutor _executor;

    // Dependency Injection injects whatever providers were registered (SQLite, MySQL, OpenAI, etc.)
    public Nl2SqlEngine(
        ISchemaExtractor schemaExtractor,
        ISqlSynthesizer synthesizer,
        ISqlGuardrail guardrail,
        IOptions<Nl2SqlOptions> options,
        ISqlExecutor executor)
    {
        _schemaExtractor = schemaExtractor;
        _synthesizer = synthesizer;
        _guardrail = guardrail;
        _options = options.Value;
        _executor = executor;
    }

    public async Task<Nl2SqlQueryResult> ExecuteQueryAsync(
        string userPrompt, 
        string connectionString, 
        UserSecurityContext? securityContext = null, 
        CancellationToken ct = default)
    {
        try
        {
            // Step 1: Extract database schema (Masking sensitive columns automatically)
            var schema = await _schemaExtractor.ExtractSchemaAsync(connectionString, securityContext, ct);

            // Step 2: Convert natural language prompt to raw SQL via LLM
            var rawSql = await _synthesizer.SynthesizeSqlAsync(userPrompt, schema, ct);

            // Step 3: Validate SQL (Must be SELECT-only, enforce max row limit)
            var (isSafe, sanitizedSql, violationError) = _guardrail.ValidateAndSecureSql(rawSql, securityContext, _options.MaxRowLimit);

            if (!isSafe)
            {
                return new Nl2SqlQueryResult(
                    IsSuccess: false, 
                    GeneratedSql: rawSql, 
                    Data: null, 
                    ErrorMessage: $"Security Violation: {violationError}");
            }

            // Step 4: Return result (Query execution step)
            var dataRows = await _executor.ExecuteReaderAsync(
                connectionString, 
                sanitizedSql, 
                _options.QueryTimeoutSeconds, 
                ct);

            return new Nl2SqlQueryResult(
                IsSuccess: true, 
                GeneratedSql: sanitizedSql, 
                Data: dataRows, 
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            return new Nl2SqlQueryResult(
                IsSuccess: false, 
                GeneratedSql: string.Empty, 
                Data: null, 
                ErrorMessage: ex.Message);
        }
    }
}