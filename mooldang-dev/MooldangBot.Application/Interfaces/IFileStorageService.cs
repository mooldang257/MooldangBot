using Microsoft.AspNetCore.Http;

namespace MooldangBot.Domain.Abstractions
{
    /// <summary>
    /// [하모니의 창고]: 파일을 시스템(로컬/클라우드)에 저장하고 경로를 관리하는 인터페이스입니다.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// 파일을 저장하고 외부에 공개 가능한 URL을 반환합니다.
        /// </summary>
        /// <param name="file">업로드할 파일</param>
        /// <param name="subDirectory">저장할 하위 디렉토리 (예: icons, backgrounds)</param>
        /// <returns>파일 접근 URL</returns>
        Task<string> SaveFileAsync(IFormFile file, string subDirectory);

        /// <summary>
        /// 바이트 배열을 저장하고 외부에 공개 가능한 URL을 반환합니다.
        /// </summary>
        /// <param name="content">파일 바이트 데이터</param>
        /// <param name="subDirectory">저장할 하위 디렉토리</param>
        /// <param name="fileName">파일명</param>
        /// <returns>파일 접근 URL</returns>
        Task<string> SaveFileAsync(byte[] content, string subDirectory, string fileName);

        /// <summary>
        /// 파일을 삭제합니다.
        /// </summary>
        /// <param name="fileUrl">삭제할 파일의 전체 URL</param>
        /// <returns>삭제 성공 여부</returns>
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
