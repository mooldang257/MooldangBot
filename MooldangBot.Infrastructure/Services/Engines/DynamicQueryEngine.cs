using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MySqlConnector;

namespace MooldangBot.Infrastructure.Services.Engines
{
    /// <summary>
    /// [v1.8] Safe Dynamic Query Engine 실구현체
    /// </summary>
    public class DynamicQueryEngine : IDynamicQueryEngine
    {
        private readonly IAppDbContext _db;
        private readonly ICommandMasterCacheService _cache;
        private readonly IDynamicVariableResolver _resolver;
        private readonly ILogger<DynamicQueryEngine> _logger;

        public DynamicQueryEngine(
            IAppDbContext db,
            ICommandMasterCacheService cache,
            IDynamicVariableResolver resolver,
            ILogger<DynamicQueryEngine> logger)
        {
            _db = db;
            _cache = cache;
            _resolver = resolver;
            _logger = logger;
        }

        public async Task<string> ProcessMessageAsync(string message, string streamerChzzkUid, string viewerUid)
        {
            if (string.IsNullOrWhiteSpace(message)) return message;
            if (!message.Contains('{') || !message.Contains('}')) return message;

            var variables = await _cache.GetFullVariablesAsync();
            if (variables == null || variables.Count == 0) return message;

            // 정규식: {내용} 형태의 모든 패턴 추출
            var matches = Regex.Matches(message, @"\{[^{}]+\}");
            
            string resultMessage = message;

            foreach (Match match in matches)
            {
                string keyword = match.Value;
                var varDef = variables.FirstOrDefault(v => v.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));

                if (varDef != null)
                {
                    try
                    {
                        string queryString = varDef.QueryString.TrimStart();
                        string? queryResult = null;

                        // [v4.4.0] METHOD: 로 시작하면 내부 리졸버 호출, 아니면 SQL 실행
                        if (queryString.StartsWith("METHOD:", StringComparison.OrdinalIgnoreCase))
                        {
                            string methodName = queryString.Substring(7).Trim(); // "METHOD:" 이후 문자열
                            queryResult = await _resolver.ResolveAsync(methodName, streamerChzzkUid, viewerUid);
                        }
                        else if (queryString.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                        {
                            // 🛡️ [보안 강화 2]: SQL Injection 방지를 위한 파라미터화 쿼리 실행
                            queryResult = await _db.Database.SqlQueryRaw<string>(
                                queryString,
                                new MySqlParameter("@streamerUid", streamerChzzkUid),
                                new MySqlParameter("@viewerUid", viewerUid),
                                new MySqlParameter("@uid", viewerUid) // 레거시/아키텍트 요청 호환용
                            ).FirstOrDefaultAsync();
                        }
                        else
                        {
                            _logger.LogWarning($"[Security Alert]: Dynamic variable {keyword} has invalid query format: {queryString}");
                            continue;
                        }

                        resultMessage = resultMessage.Replace(keyword, queryResult ?? "", StringComparison.OrdinalIgnoreCase);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[Query/Resolve Error]: Failed to process variable {keyword} (Streamer: {streamerChzzkUid}, Viewer: {viewerUid})");
                        // 에러 발생 시 키워드를 그대로 유지
                    }
                }
            }

            return resultMessage;
        }
    }
}
