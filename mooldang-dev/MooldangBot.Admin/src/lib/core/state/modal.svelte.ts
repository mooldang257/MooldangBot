// src/lib/core/state/modal.svelte.ts
/**
 * 🌠 [Osiris]: 프리미엄 확인 모달 엔진 (Premium Confirm Modal Engine)
 * 브라우저의 confirm()을 대체하여 서비스 디자인과 일관된 고품질 모달을 제공합니다.
 */
class ModalState {
    isOpen = $state(false);
    title = $state("정말로 삭제할까요?");
    message = $state("이 작업은 되돌릴 수 없으며 모든 데이터가 유실됩니다.");
    confirmText = $state("삭제하기");
    cancelText = $state("취소");
    variant = $state<"danger" | "warning" | "info">("danger");
    
    // 이행을 기다리는 Promise Resolve/Reject 저장소
    private resolve: ((value: boolean) => void) | null = null;

    /**
     * 확인 모달을 실행하고 사용자의 선택을 비동기로 기다립니다.
     * @example
     * if (await modal.confirm({ title: '정말요?' })) { ... }
     */
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

    /**
     * 사용자가 '확인' 버튼을 클릭했을 때 호출
     */
    handleConfirm() {
        this.isOpen = false;
        if (this.resolve) this.resolve(true);
        this.resolve = null;
    }

    /**
     * 사용자가 '취소' 버튼을 클릭하거나 배경을 클릭했을 때 호출
     */
    handleCancel() {
        this.isOpen = false;
        if (this.resolve) this.resolve(false);
        this.resolve = null;
    }
}

// [싱글톤]: 앱 전체에서 단 하나의 모달 상태를 관리합니다.
export const modal = new ModalState();
