// src/lib/api/client.ts
import { browser } from '$app/environment';

export interface Result<T> {
    isSuccess: boolean;
    value: T;
    error: string | null;
    responseTime: string;
}

/**
 * 표준 API Fetch 래퍼
 * Result<T> 봉투를 까서 성공 시 실제 값(T)만 반환하며, 실패 시 자동으로 예외를 던집니다.
 * 기본적으로 Content-Type(application/json) 등을 포함합니다.
 */
export async function apiFetch<T>(
    url: string, 
    options?: RequestInit & { fetch?: typeof fetch }
): Promise<T> {
    const customFetch = options?.fetch || fetch;
    
    // 기본 헤더 병합
    const defaultHeaders: HeadersInit = {
        'Content-Type': 'application/json',
        // 'Authorization': `Bearer ${선장님의_인증_토큰}` // 추후 인증 연동 시 활성화
    };

    // [Aegis Bridge]: SSR 환경에서는 hooks.server.ts의 handleFetch가 주소 치환과 쿠키 전달을 전담합니다.
    const finalUrl = url;

    const fetchOptions: RequestInit = {
        ...options,
        headers: { ...defaultHeaders, ...options?.headers },
        credentials: 'include' // 쿠키 전달을 위해 명시
    };

    const response = await customFetch(finalUrl, fetchOptions);
    
    // HTTP 상태 코드가 2xx가 아닌 경우에 대한 기본 예외 처리
    if (!response.ok) {
        throw new Error(`HTTP Error: ${response.status}`);
    }

    const result: Result<T> = await response.json().catch(err => {
        console.error("[apiFetch] JSON 파싱 실패:", err, "Response:", response);
        throw new Error("물댕봇 데이터 형식 오류");
    });
    
    if (!result.isSuccess) {
        console.warn("[apiFetch] 비즈니스 로직 실패:", result.error);
        throw new Error(result.error || '물댕봇 통신 오류 발생');
    }
    
    return result.value;
}
