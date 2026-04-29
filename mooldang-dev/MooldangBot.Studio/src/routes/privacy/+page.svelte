<script lang="ts">
    import { fade, fly } from 'svelte/transition';
</script>

<svelte:head>
    <title>개인정보처리방침 - 물댕봇</title>
</svelte:head>

<div class="max-w-4xl mx-auto px-6 py-20 md:py-32" in:fade>
    <div class="mb-16" in:fly={{ y: 20, duration: 800 }}>
        <h1 class="text-4xl md:text-5xl font-[1000] text-slate-800 tracking-tighter mb-4">개인정보처리방침</h1>
        <p class="text-slate-500 font-bold uppercase tracking-widest text-xs">Privacy Policy</p>
    </div>

    <div class="space-y-12 text-slate-600 leading-relaxed font-medium">
        <!-- 1. 처리 목적 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-6 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                1. 개인정보 처리의 목적
            </h2>
            <p class="mb-4">MOOLDANG봇(이하 “서비스”)은 다음의 목적을 위하여 개인정보를 처리하고 있으며, 목적 이외의 용도로는 이용하지 않습니다. 목적이 변경될 시에는 이용자에게 사전 동의를 구할 예정입니다.</p>
            <ul class="list-disc ml-6 space-y-3">
                <li><strong>서비스 회원가입 및 관리</strong>: 가입 의사 확인, 본인 식별·인증, 회원 자격 유지·관리, 각종 고지·통지, 서비스 부정이용 방지</li>
                <li><strong>민원 및 고충 처리</strong>: 이용자의 신원 확인, 민원 사항 확인, 사실 조사를 위한 연락·통지, 처리 결과 통보</li>
                <li><strong>서비스 제공 및 정산</strong>: 맞춤형 봇 서비스 제공, 포인트 및 요금 정산, 상담 관리</li>
                <li><strong>부정 이용 방지</strong>: 계정 도용 및 비정상적인 서비스 접근 방지</li>
            </ul>
        </section>

        <!-- 2. 보유 기간 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-6 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                2. 개인정보 처리 및 보유 기간
            </h2>
            <p class="mb-4">서비스는 이용자로부터 개인정보를 수집할 시에 동의받은 보유 및 이용 기간 내에서 개인정보를 처리하고 보유합니다.</p>
            <p class="mb-4">원칙적으로 <strong>회원 탈퇴(서비스 이용계약 해지) 시까지</strong> 위 이용 목적을 위하여 보유·이용됩니다. 단, 관련 법령에 따라 일정 기간 보관이 필요한 경우 아래와 같이 보관합니다:</p>
            <div class="bg-slate-50 p-6 rounded-2xl border border-slate-100 grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                    <p class="text-xs font-black text-slate-400 mb-2 uppercase tracking-wider">소비자 불만 또는 분쟁 처리</p>
                    <p class="text-sm font-bold text-slate-700">3년 (전자상거래법)</p>
                </div>
                <div>
                    <p class="text-xs font-black text-slate-400 mb-2 uppercase tracking-wider">표시·광고에 관한 기록</p>
                    <p class="text-sm font-bold text-slate-700">6개월 (전자상거래법)</p>
                </div>
                <div>
                    <p class="text-xs font-black text-slate-400 mb-2 uppercase tracking-wider">로그기록 (방문기록)</p>
                    <p class="text-sm font-bold text-slate-700">3개월 (통신비밀보호법)</p>
                </div>
                <div>
                    <p class="text-xs font-black text-slate-400 mb-2 uppercase tracking-wider">계약 또는 청약철회 등</p>
                    <p class="text-sm font-bold text-slate-700">5년 (전자상거래법)</p>
                </div>
            </div>
        </section>

        <!-- 3. 정보주체의 권리 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-6 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                3. 정보주체의 권리·의무 및 행사방법
            </h2>
            <ul class="list-disc ml-6 space-y-2 text-sm">
                <li>이용자는 개인정보주체로서 언제든지 개인정보 열람, 정정, 삭제, 처리정지 요구 등의 권리를 행사할 수 있습니다.</li>
                <li>권리 행사는 전자우편 등을 통하여 하실 수 있으며, 서비스는 이에 대해 지체 없이 조치하겠습니다.</li>
                <li>정보주체가 오류 정정을 요구한 경우, 정정이 완료될 때까지 해당 개인정보를 이용하거나 제공하지 않습니다.</li>
                <li>권리 행사는 법정대리인이나 위임을 받은 자 등 대리인을 통해서도 가능합니다.</li>
            </ul>
        </section>

        <!-- 4. 수집 항목 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-6 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                4. 처리하는 개인정보 항목 및 수집 방법
            </h2>
            <div class="space-y-4">
                <div class="p-5 bg-white border border-slate-200 rounded-2xl shadow-sm">
                    <p class="font-bold text-slate-800 mb-2">필수 항목</p>
                    <p class="text-sm text-slate-500 leading-relaxed">치지직(CHZZK) 서비스 내 사용자 고유 식별 번호(UID), 닉네임, 프로필 이미지 정보</p>
                </div>
                <div class="p-5 bg-white border border-slate-200 rounded-2xl shadow-sm">
                    <p class="font-bold text-slate-800 mb-2">추가 수집 가능 항목 (서비스 이용 과정)</p>
                    <p class="text-sm text-slate-500 leading-relaxed">전자우편 주소, 결제 기록(후원, 포인트 엔진 이용 등), 서비스 이용 기록, 명령어 호출 및 접속 로그</p>
                </div>
            </div>
        </section>

        <!-- 5. 제3자 제공 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-6 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                5. 개인정보의 제3자 제공
            </h2>
            <p class="mb-4">서비스는 이용자의 개인정보를 원칙적으로 외부에 제공하지 않습니다. 단, 아래의 경우는 예외로 합니다.</p>
            <ul class="list-disc ml-6 space-y-2 text-sm">
                <li>이용자가 사전에 제3자 제공에 동의한 경우</li>
                <li>법령의 규정에 의거하거나, 수사 목적으로 법령에 정해진 절차와 방법에 따라 수사기관의 요구가 있는 경우</li>
            </ul>
        </section>

        <!-- 6. 개인정보 파기 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-6 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                6. 개인정보의 파기
            </h2>
            <p class="mb-4">서비스는 원칙적으로 개인정보 처리 목적이 달성된 경우에는 지체 없이 해당 개인정보를 파기합니다.</p>
            <p class="text-sm bg-slate-50 p-5 rounded-2xl border border-slate-100 leading-relaxed">
                <strong>파기 절차 및 방법</strong>: 전자적 파일 형태의 정보는 기록을 재생할 수 없는 기술적 방법을 사용하여 안전하게 삭제하며, 종이 문서에 출력된 개인정보는 분쇄기로 분쇄하거나 소각하여 파기합니다.
            </p>
        </section>

        <!-- 7. 자동 수집 장치 (챗봇) -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-6 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                7. 개인정보 자동 수집 장치의 설치·운영 및 거부
            </h2>
            <p class="mb-4">서비스는 개별적인 맞춤 환경을 제공하기 위해 이용 정보를 저장하고 수시로 불러오는 채팅 봇을 사용합니다.</p>
            <ul class="list-disc ml-6 space-y-3 text-sm">
                <li>치지직 채팅창을 통해 MOOLDANG봇의 이름으로 직접 안내 및 결과가 출력되며, 이 과정에서 봇을 통해 전송된 이용 정보는 플랫폼 및 환경에 따라 다른 사용자에게 공개될 수 있습니다.</li>
                <li>이용자는 채팅 봇의 정보 저장을 거부할 수 있으나, 이 경우 전체 또는 일부 서비스 이용에 제한이 있을 수 있습니다.</li>
            </ul>
        </section>

        <!-- 8. 책임자 정보 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-6 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                8. 개인정보 보호 책임자 (Privacy Officer)
            </h2>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div class="p-6 bg-slate-900 rounded-[2rem] text-white shadow-xl shadow-slate-200">
                    <p class="text-[10px] font-black text-primary uppercase tracking-[0.2em] mb-4">Privacy Officer</p>
                    <div class="space-y-1">
                        <p class="text-lg font-black">물댕 (Mooldang)</p>
                        <p class="text-sm text-slate-400 font-bold">mooldang@naver.com</p>
                    </div>
                </div>
                <div class="flex flex-col justify-center px-4">
                    <p class="text-sm text-slate-500 font-medium leading-relaxed">
                        서비스 이용 중 발생하는 모든 개인정보 보호 관련 문의나 불만 처리는 위 연락처로 문의해 주시기 바랍니다. 신속하고 성실하게 답변해 드리겠습니다.
                    </p>
                </div>
            </div>
        </section>
        
        <div class="pt-12 border-t border-slate-200 flex flex-col md:flex-row justify-between items-center gap-4">
            <p class="text-xs text-slate-400 font-bold">시행일자: 2026년 4월 29일</p>
            <p class="text-[10px] font-black text-primary uppercase tracking-widest">MooldangBot Policy Center</p>
        </div>
    </div>
</div>
