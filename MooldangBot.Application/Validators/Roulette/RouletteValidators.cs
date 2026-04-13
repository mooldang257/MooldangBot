using FluentValidation;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Application.Validators.Roulette;

/// <summary>
/// 🎰 [이지스의 정화]: 룰렛 업데이트 요청에 대한 정밀 검증을 수행합니다.
/// </summary>
public class RouletteUpdateRequestValidator : AbstractValidator<RouletteUpdateRequest>
{
    public RouletteUpdateRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("유효하지 않은 룰렛 ID입니다.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("룰렛 이름은 비어 있을 수 없습니다.")
            .MaximumLength(50).WithMessage("룰렛 이름은 50자를 초과할 수 없습니다.");

        RuleFor(x => x.CostPerSpin)
            .GreaterThanOrEqualTo(0).WithMessage("회당 비용은 0 이상이어야 합니다.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("최소 하나 이상의 룰렛 아이템이 필요합니다.")
            .Must(items => items.Sum(i => i.Probability) > 0)
            .WithMessage("아이템들의 총 확률 합계가 0보다 커야 합니다.");
    }
}

/// <summary>
/// 🎶 [이지스의 소통]: 애니메이션 완료 요청에 대한 구문 검증을 수행합니다.
/// </summary>
public class CompleteRequestValidator : AbstractValidator<CompleteRequest>
{
    public CompleteRequestValidator()
    {
        RuleFor(x => x.SpinId)
            .NotEmpty().WithMessage("SpinId는 필수 값입니다.")
            .Length(36).WithMessage("유효한 UUID 형식의 SpinId가 필요합니다.");
    }
}
