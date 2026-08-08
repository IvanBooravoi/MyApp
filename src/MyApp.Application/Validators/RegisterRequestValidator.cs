using FluentValidation;
using MyApp.Application.DTO;

namespace MyApp.Application.Validators {
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest> {
        public RegisterRequestValidator() {
            RuleFor(x => x.UserName).NotEmpty().MinimumLength(3);
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.Password).MinimumLength(6);
        }
    }
}
