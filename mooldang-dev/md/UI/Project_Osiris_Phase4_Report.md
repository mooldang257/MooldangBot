# [Project Osiris Phase 4]: Cloudflare Tunnel Optimization Detailed Report (Zero Trust)

본 단계는 클라우드플레어 터널(Argo) 환경의 보안적 이점을 극대화하기 위해, 기존의 복잡한 SSL 설정을 제거하고 **내부망 신뢰(Internal Trust) 기반의 리얼 IP 추출 로직**을 적용하여 **Zero Trust** 아키텍처를 완성하는 것을 목표로 했습니다. 시니어 파트너 '물멍'의 아키텍처 분석을 바탕으로 가장 효율적이고 강력한 보안 체계를 구축했습니다.

## 🚀 아키텍처 핵심 요약 (Zero Trust)
- **Zero Attack Surface**: 외부로 노출되는 인바운드 포트(80, 443) 없이 아웃바운드 터널만으로 서비스를 운영합니다.
- **Edge SSL Offloading**: SSL 인증서 발급/갱신을 클라우드플레어 엣지로 위임하여 서버 측 리소스 소모를 최소화했습니다.
- **Internal Network Trust**: 터널 컨테이너(`cloudflared`)가 위치한 Docker 내부 서브넷을 신뢰 대상으로 지정하여, 별도의 외부 IP 갱신 스크립트 없이도 시청자의 실제 IP를 확보합니다.

---

## 🛠️ 핵심 구현 코드 스니펫

### 1. [Nginx] 내부망 신뢰 및 리얼 IP 추출 설정 (`nginx/nginx.conf`)
시니어 파트너의 '내부망 신뢰' 제언을 반영하여 cloudflared가 전달하는 실제 IP를 추출합니다.

```nginx
# [오시리스의 눈]: 클라우드플레어 터널(cloudflared) 환경 최적화
# 터널 컨테이너가 위치한 Docker 내부 네트워크 대역(일반적으로 172.16.x.x)을 신뢰합니다.
set_real_ip_from 172.16.0.0/12; 
set_real_ip_from 192.168.0.0/16; 
real_ip_header CF-Connecting-IP; # 클라우드플레어가 전달하는 실제 시청자 IP

http {
    # ... 공통 프록시 헤더 설정 ...
    server {
        listen 80;
        
        # [오시리스의 공명]: 하위 서비스로 리얼 IP 및 프로토콜 체인 전달
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme; # HTTPS 여부 정확히 전달
        
        # ... 각 서비스(admin, api, overlay) 라우팅 ...
    }
}
```

---

## 🎞️ 서비스 통합 및 운영 최적화
불필요한 Certbot 서비스를 제거하여 전체 인프라를 더 가볍고 관리하기 쉽게 재구성했습니다.

- **[계획 취소]**: `certbot` 컨테이너 추가 및 `ssl_init.ps1` 생성 계획은 Zero Trust 환경에 맞지 않아 과감히 폐기했습니다.
- **[오케스트레이션]**: `docker-compose.yml`은 기존의 80번 포트 구조를 유지하되, 클라우드플레어 터널 설정(Public Hostname)을 통해 모든 HTTPS 트래픽을 Nginx로 라우팅합니다.

## 🛠️ 확인 방법

### 1. 설정 검증
- `nginx -t`를 통해 내부망 대역 설정의 문법적 무결성을 확인했습니다.

### 2. 구동 테스트 가이드
- 클라우드플레어 Zero Trust 대시보드에서 `mooldang.store` 도메인을 `http://mooldang-nginx:80`으로 연결합니다.
- 백엔드(.NET) 로그의 `X-Real-IP` 필드에 시청자의 실제 공인 IP가 기록되는지 확인합니다.
- 오버레이 대시보드와 OBS 화면에서 `wss://` 연결이 끊김 없이 안정적으로 연동되는지 최종 검증합니다.

## 💡 최종 제언 (Next Steps)
1. **Access 정책 강화**: 클라우드플레어 Zero Trust의 **Access 정책**을 활용하여 특정 이메일이나 지역에서만 대시보드(`admin`)에 접근할 수 있도록 2차 방어막 구축을 권장합니다.
2. **Rate Limiting**: 터널 앞단의 클라우드플레어 WAF 설정을 통해 악의적인 트래픽에 대한 속도 제한을 적용하면 더 완벽한 요새가 됩니다.
