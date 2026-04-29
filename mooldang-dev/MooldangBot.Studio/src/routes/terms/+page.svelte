<script lang="ts">
    import { fade, fly } from 'svelte/transition';
</script>

<svelte:head>
    <title>이용약관 - 물댕봇</title>
</svelte:head>

<div class="max-w-4xl mx-auto px-6 py-20 md:py-32" in:fade>
    <div class="mb-16" in:fly={{ y: 20, duration: 800 }}>
        <h1 class="text-4xl md:text-5xl font-[1000] text-slate-800 tracking-tighter mb-4">이용약관</h1>
        <p class="text-slate-500 font-bold uppercase tracking-widest text-xs">Terms of Service (Full Version)</p>
    </div>

    <div class="space-y-16 text-slate-600 leading-relaxed font-medium">
        
        <!-- 제1장 총칙 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-8 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                제 1 장 총칙
            </h2>
            <div class="space-y-6">
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 1 조 (목적)</h3>
                    <p class="text-sm">이 약관은 물댕 Mooldang(이하 “개발자”)이 제공하는 채팅 봇 서비스 및 이에 부수하는 네트워크, 웹사이트, 기타 서비스(이하 “서비스”)의 이용에 대한 개발자와 이용자의 권리·의무 및 책임사항을 규정함을 목적으로 합니다.</p>
                </div>
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 2 조 (용어의 정의)</h3>
                    <p class="text-sm mb-2">“채팅 봇”이란 개발자가 제공하는 상호작용 소프트웨어를 의미하며, “계정 정보”란 이용자의 UID, 별명, 프로필 사진 등을 통칭합니다.</p>
                </div>
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 4 조 (약관의 효력 및 변경)</h3>
                    <p class="text-sm">개발자는 관련 법령을 위배하지 않는 범위에서 약관을 개정할 수 있으며, 중요한 변경 사항은 적용 7일(불리한 경우 30일) 전부터 공지합니다.</p>
                </div>
            </div>
        </section>

        <!-- 제2장 개인정보 관리 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-8 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                제 2 장 개인정보 관리
            </h2>
            <div class="space-y-6">
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 8 조 (개인정보의 보호 및 사용)</h3>
                    <p class="text-sm">개발자는 관련 법령 및 개인정보처리방침에 따라 회원의 개인정보를 보호하기 위해 노력합니다. 단, 서비스의 특성에 따라 캐릭터 정보나 별명 등 자신을 소개하는 내용은 공개될 수 있습니다.</p>
                </div>
            </div>
        </section>

        <!-- 제3장 당사자의 의무 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-8 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                제 3 장 이용계약 당사자의 의무
            </h2>
            <div class="space-y-6">
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 10 조 (회원의 의무)</h3>
                    <p class="text-sm mb-2">회원은 허위 사실 기재, 비정상적인 방법으로 사이버 자산 취득, 타인 명의 도용, 악성 코드 전송 등을 해서는 안 됩니다.</p>
                    <p class="text-xs text-rose-500 font-bold">* 위반 시 계정 이용이 제한될 수 있습니다.</p>
                </div>
            </div>
        </section>

        <!-- 제4장 서비스 이용 및 제한 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-8 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                제 4 장 서비스 이용 및 제한
            </h2>
            <div class="space-y-6">
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 13 조 (서비스의 변경 및 중단)</h3>
                    <p class="text-sm">개발자는 경영상의 중대한 사유로 서비스를 중단할 수 있으며, 이 경우 30일 전 홈페이지를 통해 공지합니다.</p>
                </div>
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 16 조 (저작권의 귀속)</h3>
                    <p class="text-sm">서비스 내 콘텐츠에 대한 저작권은 개발자에게 귀속됩니다. 이용자는 영리 목적으로 이를 복제하여 사용할 수 없습니다.</p>
                </div>
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 18 조~20 조 (이용 제한 및 이의 신청)</h3>
                    <p class="text-sm">의무 위반 시 일부 권한 제한 또는 영구 이용 제한 조치를 취할 수 있으며, 회원은 통지일로부터 14일 이내에 이의 신청을 할 수 있습니다.</p>
                </div>
            </div>
        </section>

        <!-- 제5장 대금 결제 및 환불 (미래 대비) -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-8 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                제 5 장 대금 결제 및 환불
            </h2>
            <div class="space-y-6">
                <div class="p-5 bg-amber-50/50 border border-amber-100 rounded-2xl">
                    <h3 class="font-black text-amber-800 mb-2">제 22 조 (청약 철회)</h3>
                    <p class="text-sm text-amber-900/70">유료 콘텐츠를 구매한 회원은 이용 가능일로부터 7일 이내에 청약 철회를 할 수 있습니다. 단, 구매 즉시 사용되거나 혜택이 적용된 콘텐츠는 철회가 제한됩니다.</p>
                </div>
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 23 조 (과오납금 환급)</h3>
                    <p class="text-sm">과오납금 발생 시 개발자는 이를 환급하며, 회원의 과실로 발생한 경우 환급 비용은 회원이 부담합니다.</p>
                </div>
            </div>
        </section>

        <!-- 제6장 손해배상 및 기타 -->
        <section>
            <h2 class="text-xl font-black text-slate-800 mb-8 tracking-tight flex items-center gap-2">
                <span class="w-1.5 h-6 bg-primary rounded-full"></span>
                제 6 장 손해배상 및 면책조항
            </h2>
            <div class="space-y-6">
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 26 조 (개발자의 면책)</h3>
                    <p class="text-sm">천재지변, 서비스 설비 보수/교체 등으로 인한 불가피한 손해, 무료 서비스 이용 중 발생한 손해에 대해서는 고의 또는 중과실이 없는 한 책임을 지지 않습니다.</p>
                </div>
                <div>
                    <h3 class="font-black text-slate-800 mb-2">제 28 조 (재판권 및 준거법)</h3>
                    <p class="text-sm">본 약관은 대한민국 법률에 따라 규율되며, 분쟁 발생 시 관련 법령에 따른 법원을 관할 법원으로 합니다.</p>
                </div>
            </div>
        </section>

        <div class="pt-12 border-t border-slate-200 flex flex-col md:flex-row justify-between items-center gap-4">
            <div class="text-left">
                <p class="text-xs text-slate-400 font-bold mb-1">공고일자: 2026년 4월 29일</p>
                <p class="text-xs text-slate-400 font-bold">시행일자: 2026년 4월 29일</p>
            </div>
            <div class="flex flex-col items-end">
                <p class="text-[10px] font-black text-primary uppercase tracking-widest mb-1">MooldangBot Premium Service</p>
                <p class="text-[11px] font-bold text-slate-400">&copy; 2026 물댕 Mooldang. All rights reserved.</p>
            </div>
        </div>
    </div>
</div>
