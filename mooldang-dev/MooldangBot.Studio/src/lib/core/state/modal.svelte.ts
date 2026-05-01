// src/lib/core/state/modal.svelte.ts
/**
 * 🌠 [물멍]: 프리미엄 확인 모달 엔진 (Premium Confirm Modal Engine)
 * 브라우저의 confirm()을 대체하여 서비스 디자인과 일관된 고품질 모달을 제공합니다.
 */
class ModalState {
    isOpen = $state(false);
    title = $state("정말로 삭제할까요?");
    message = $state("이 작업은 되돌릴 수 없으며 모든 데이터가 유실됩니다.");
    confirmText = $state("삭제하기");
    cancelText = $state("취소");
    variant = $state<"danger" | "warning" | "info">("danger");
    
    private resolve: ((value: boolean) => void) | null = null;

    async confirm(options: {
        title?: string;
        message?: string;
        confirmText?: string;
        cancelText?: string;
        variant?: "danger" | "warning" | "info";
    }): Promise<boolean> {
        this.title = options.title ?? "정말로 삭제할까요?";
        this.message = options.message ?? "이 작업은 되돌릴 수 없으며 모든 데이터가 유실됩니다.";
        this.confirmText = options.confirmText ?? "삭제하기";
        this.cancelText = options.cancelText ?? "취소";
        this.variant = options.variant ?? "danger";
        this.isOpen = true;

        return new Promise((resolve) => {
            this.resolve = resolve;
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
