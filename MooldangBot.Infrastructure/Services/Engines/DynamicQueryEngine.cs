using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;
using MySqlConnector;
using MooldangBot.Contracts.Security;

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

        public async Task<string> ProcessMessageAsync(string message, string streamerChzzkUid, string viewerUid, string? viewerName = null)
        {
            if (string.IsNullOrWhiteSpace(message)) return message;

            // [v19.0] $(닉네임) 변수 통합 처리
            string resultMessage = message;
            if (!string.IsNullOrEmpty(viewerName))
            {
                resultMessage = resultMessage.Replace("$(닉네임)", viewerName, StringComparison.OrdinalIgnoreCase);
            }

            if (!resultMessage.Contains("$(") || !resultMessage.Contains(')')) return resultMessage;

            var variables = await _cache.GetFullVariablesAsync();
            if (variables == null || variables.Count == 0) return resultMessage;

            // 1. [변수 추출]: $(내용) 형태의 모든 패턴 추출 (Regex: \$\((?<varName>.*?)\))
            var matches = Regex.Matches(resultMessage, @"\$\((?<varName>.*?)\)").Cast<Match>().ToList();
            if (!matches.Any()) return resultMessage;

            // 2. [병렬 공명]: 모든 변수 치환 작업을 동시에 실행 (Resonance Parallelism)
            var resolutionTasks = matches.Select(async match =>
            {
                string keyword = match.Value;
                var varDef = variables.FirstOrDefault(v => v.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));

                if (varDef == null) return new { Keyword = keyword, Replacement = keyword };

                try
                {
                    string queryString = varDef.QueryString.TrimStart();
                    string? queryResult = null;

                    if (queryString.StartsWith("METHOD:", StringComparison.OrdinalIgnoreCase))
                    {
                        string methodName = queryString.Substring(7).Trim();
                        queryResult = await _resolver.ResolveAsync(methodName, streamerChzzkUid, viewerUid, viewerName);
                    }
                    else if (queryString.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!IsQuerySafe(queryString)) return new { Keyword = keyword, Replacement = keyword };

                        var viewerHash = Sha256Hasher.ComputeHash(viewerUid);
                        queryResult = await _db.Database.SqlQueryRaw<string>(
                            queryString,
                            new MySqlParameter("@streamerUid", streamerChzzkUid),
                            new MySqlParameter("@viewerUid", viewerUid),
                            new MySqlParameter("@viewerHash", viewerHash),
                            new MySqlParameter("@uid", viewerUid)
                        ).FirstOrDefaultAsync();
                    }

                    return new { Keyword = keyword, Replacement = queryResult ?? "" };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Query/Resolve Error]: Failed to process variable {keyword}");
                    return new { Keyword = keyword, Replacement = keyword };
                }
            });

            var resolvedResults = await Task.WhenAll(resolutionTasks);

            // 3. [일괄 적용]: 모든 결과를 한 번에 반영
            foreach (var res in resolvedResults)
            {
                resultMessage = resultMessage.Replace(res.Keyword, res.Replacement, StringComparison.OrdinalIgnoreCase);
            }

            return resultMessage;
        }

        /// <summary>
        /// 🛡️ [오시리스의 방패]: 실행 가능한 SQL 쿼리가 안전한지(Read-only) 검증합니다.
        /// </summary>
        private bool IsQuerySafe(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;

            // 1. 반드시 SELECT로 시작해야 함 (Case-insensitive)
            if (!query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) return false;

            // 2. 다중 구문 실행 방지 (세미콜론 차단)
            if (query.Contains(';')) return false;

            // 3. 위험한 키워드 블랙리스트 (정규식으로 단어 단위 매칭)
            string[] forbiddenKeywords = { 
                "DROP", "DELETE", "UPDATE", "INSERT", "TRUNCATE", "ALTER", 
                "GRANT", "REVOKE", "REPLACE", "CREATE", "RENAME", "SCHEMA", "DATABASE" 
            };

            foreach (var word in forbiddenKeywords)
            {
                // 소문자/대문자 섞인 경우 방지 및 정확한 단어 매칭
                if (Regex.IsMatch(query, $@"\b{word}\b", RegexOptions.IgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
