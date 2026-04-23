using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MooldangBot.Application.Common.Interfaces;

/// <summary>
/// [v19.0] 노래책 대량 관리를 위한 엑셀 고속 처리 인터페이스
/// </summary>
public interface ISongBookExcelService
{
    /// <summary>
    /// 스트리머의 현재 노래책 데이터를 엑셀 스트림으로 내보냅니다.
    /// (템플릿 용도로도 사용 가능)
    /// </summary>
    Task<Stream> ExportSongBookAsync(int streamerProfileId);

    /// <summary>
    /// 업로드된 엑셀 파일을 읽어 노래책에 일괄 등록합니다.
    /// (지능형 매칭 및 중복 방지 로직 포함)
    /// </summary>
    Task<SongBookImportResultDto> ImportSongBookAsync(int streamerProfileId, Stream excelStream);
}

public class SongBookImportResultDto
{
    public int SuccessCount { get; set; }
    public int TotalCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// 엑셀 파일 내 행 데이터를 표현하는 DTO (MiniExcel 매핑용)
/// </summary>
public class SongBookExcelRow
{
    public string? Title { get; set; }      // 필수
    public string? Artist { get; set; }     // 선택
    public string? Category { get; set; }   // 선택
    public string? Pitch { get; set; }      // 선택 (예: -2, 원키)
    public string? Proficiency { get; set; } // 선택 (예: 연습중)
    public string? YoutubeUrl { get; set; }  // 선택
    public string? Alias { get; set; }       // 선택
}
