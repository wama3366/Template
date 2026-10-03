using FluentValidation;
using LibraryTemplate.Application.BackgroundJobs;
using LibraryTemplate.Application.Events;
using LibraryTemplate.Application.Services.Books;
using LibraryTemplate.Application.Services.Branches;
using LibraryTemplate.Application.Services.Loans;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryTemplate.Application;

public static class ApplicationServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<StartLoanAppRequestValidator>();

        services.AddScoped<IBranchAppService, BranchAppService>();
        services.AddScoped<IBookAppService, BookAppService>();
        services.AddScoped<ILoanAppService, LoanAppService>();
        services.AddScoped<IOverdueLoanReminderJob, OverdueLoanReminderJob>();
        services.AddScoped<LibraryCatalogDomainService>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler<LoanStartedEvent>, LoanStartedEventHandler>();

        return services;
    }
}
