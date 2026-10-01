using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Services;
using FluentValidation;

namespace ApartmentManagementSystem.Validators
{
    // Shape checks only (required, length, range, format). Rules that need the database,
    // like "apartment already has an active lease", stay in the services.

    public class ApartmentValidator : AbstractValidator<Apartment>
    {
        public ApartmentValidator()
        {
            RuleFor(a => a.Name).NotEmpty().MaximumLength(100);
            RuleFor(a => a.Location).NotEmpty().MaximumLength(100);
            RuleFor(a => a.Address).NotEmpty().MaximumLength(200);
            RuleFor(a => a.Rent).GreaterThan(0);
            RuleFor(a => a.Rooms).InclusiveBetween(1, 20);
        }
    }

    public class TenantCreateValidator : AbstractValidator<TenantCreateDto>
    {
        public TenantCreateValidator()
        {
            RuleFor(t => t.Name).NotEmpty().MaximumLength(100);
            RuleFor(t => t.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(t => t.PhoneNumber).NotEmpty().Matches(@"^\+?[0-9 \-]{7,20}$")
                .WithMessage("Phone number must be 7 to 20 digits (spaces, dashes and a leading + are allowed)");
            RuleFor(t => t.PermanentAddress).NotEmpty().MaximumLength(200);
            RuleFor(t => t.MoveInDate).NotEmpty();
            RuleFor(t => t.ApartmentId).GreaterThan(0);
        }
    }

    public class TenantUpdateValidator : AbstractValidator<TenantUpdateDto>
    {
        public TenantUpdateValidator()
        {
            RuleFor(t => t.Id).GreaterThan(0);
            RuleFor(t => t.Name).NotEmpty().MaximumLength(100);
            RuleFor(t => t.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(t => t.PhoneNumber).NotEmpty().Matches(@"^\+?[0-9 \-]{7,20}$")
                .WithMessage("Phone number must be 7 to 20 digits (spaces, dashes and a leading + are allowed)");
            RuleFor(t => t.PermanentAddress).NotEmpty().MaximumLength(200);
            RuleFor(t => t.MoveInDate).NotEmpty();
            RuleFor(t => t.MoveOutDate).GreaterThan(t => t.MoveInDate)
                .When(t => t.MoveOutDate.HasValue)
                .WithMessage("Move-out date must be after move-in date");
            RuleFor(t => t.ApartmentId).GreaterThan(0);
        }
    }

    public class LeaseCreateValidator : AbstractValidator<LeaseCreateDto>
    {
        public LeaseCreateValidator()
        {
            RuleFor(l => l.TenantId).GreaterThan(0);
            RuleFor(l => l.ApartmentId).GreaterThan(0);
            RuleFor(l => l.StartDate).NotEmpty();
            RuleFor(l => l.EndDate).GreaterThan(l => l.StartDate).WithMessage("End date must be after start date");
            RuleFor(l => l.MonthlyRent).GreaterThan(0);
            RuleFor(l => l.SecurityDeposit).GreaterThanOrEqualTo(0);
        }
    }

    public class LeaseTerminateValidator : AbstractValidator<LeaseTerminateDto>
    {
        public LeaseTerminateValidator()
        {
            RuleFor(l => l.TerminatedDate).NotEmpty();
        }
    }

    public class PaymentCreateValidator : AbstractValidator<PaymentCreateDto>
    {
        public PaymentCreateValidator()
        {
            RuleFor(p => p.Amount).GreaterThan(0);
            RuleFor(p => p.PaidDate).NotEmpty();
            RuleFor(p => p.Method).IsInEnum();
            RuleFor(p => p.ReferenceNumber).MaximumLength(100);
            // Bank transfers and e-wallets always come with a reference; cash doesn't
            RuleFor(p => p.ReferenceNumber).NotEmpty()
                .When(p => p.Method != PaymentMethod.Cash)
                .WithMessage("Reference number is required for bank transfer and e-wallet payments");
        }
    }

    public class MaintenanceCreateValidator : AbstractValidator<MaintenanceCreateDto>
    {
        public MaintenanceCreateValidator()
        {
            RuleFor(m => m.Title).NotEmpty().MaximumLength(MaintenanceService.MaxTitleLength);
            RuleFor(m => m.Description).NotEmpty().MaximumLength(MaintenanceService.MaxDescriptionLength);
            RuleFor(m => m.Priority).IsInEnum();
            RuleFor(m => m.ApartmentId).GreaterThan(0).When(m => m.ApartmentId.HasValue);
        }
    }

    public class MaintenanceStatusUpdateValidator : AbstractValidator<MaintenanceStatusUpdateDto>
    {
        public MaintenanceStatusUpdateValidator()
        {
            RuleFor(m => m.Status).IsInEnum();
            RuleFor(m => m.ResolutionNotes).NotEmpty()
                .When(m => m.Status == MaintenanceStatus.Resolved)
                .WithMessage("Resolution notes are required when resolving a request");
            RuleFor(m => m.ResolutionNotes).MaximumLength(MaintenanceService.MaxDescriptionLength);
        }
    }

    public class LoginValidator : AbstractValidator<LoginDto>
    {
        public LoginValidator()
        {
            RuleFor(l => l.Email).NotEmpty();
            RuleFor(l => l.Password).NotEmpty();
        }
    }

    public class RefreshRequestValidator : AbstractValidator<RefreshRequestDto>
    {
        public RefreshRequestValidator()
        {
            RuleFor(r => r.RefreshToken).NotEmpty();
        }
    }

    // Password strength is checked by Identity's own rules when the account is created
    public class RegisterTenantValidator : AbstractValidator<RegisterTenantDto>
    {
        public RegisterTenantValidator()
        {
            RuleFor(r => r.TenantId).GreaterThan(0);
            RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(r => r.Password).NotEmpty();
        }
    }
}
