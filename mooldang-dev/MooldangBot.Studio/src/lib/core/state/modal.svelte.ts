// src/lib/core/state/modal.svelte.ts
/**
 * 🌠 [물멍]: 프리미엄 확인 모달 엔진 (Premium Confirm Modal Engine)
 * 브라우저의 confirm()을 대체하여 서비스 디자인과 일관된 고품질 모달을 제공합니다.
 */
class ModalState {
    isOpen = $state(false);
    isAlert = $state(false);
    style = $state<"classic" | "mooldang">("classic"); // 스타일 선택 옵션 추가
    title = $state("알림");
    message = $state("");
    confirmText = $state("확인");
    cancelText = $state("취소");
    variant = $state<"danger" | "warning" | "info">("info");
    
    private resolve: ((value: boolean) => void) | null = null;

    /**
     * ⚠️ [물멍]: 확인/취소 선택형 모달
     */
    async confirm(options: {
        title?: string;
        message?: string;
        confirmText?: string;
        cancelText?: string;
        variant?: "danger" | "warning" | "info";
        style?: "classic" | "mooldang";
    }): Promise<boolean> {
        this.isAlert = false;
        this.style = options.style ?? "classic";
        this.title = options.title ?? "확인해 주세요";
        this.message = options.message ?? "";
        this.confirmText = options.confirmText ?? "확인";
        this.cancelText = options.cancelText ?? "취소";
        this.variant = options.variant ?? "info";
        this.isOpen = true;

        return new Promise((resolve) => {
            this.resolve = resolve;
        });
    }

    /**
     * 💡 [물멍]: 확인 버튼만 있는 단순 알림 모달
     */
    async alert(options: {
        title?: string;
        message?: string;
        confirmText?: string;
        variant?: "danger" | "warning" | "info";
        style?: "classic" | "mooldang";
    }): Promise<void> {
        this.isAlert = true;
        this.style = options.style ?? "classic";
        this.title = options.title ?? "알림";
        this.message = options.message ?? "";
        this.confirmText = options.confirmText ?? "확인";
        this.variant = options.variant ?? "info";
        this.isOpen = true;

        return new Promise((resolve) => {
            this.resolve = () => resolve();
        });
    }

    handleConfirm() {
        this.isOpen = false;
        if (this.resolve) this.resolve(true);
        this.resolve = null;
    }

    handleCancel() {
        this.isOpen = false;
        if (this.resolve) this.resolve(false);
        this.resolve = null;
    }
}

export const modal = new ModalState();
