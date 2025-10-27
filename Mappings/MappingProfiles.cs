using AutoMapper;
using Invoqs.API.DTOs;
using Invoqs.API.Models;

namespace Invoqs.API.Mappings;

/// <summary>
/// AutoMapper profiles for entity-DTO mapping
/// </summary>
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateCustomerMappings();
        CreateJobMappings();
        CreateInvoiceMappings();
        CreateUserMappings();
        CreateReceiptMappings();
    }

    private void CreateCustomerMappings()
    {
        // Customer Entity -> CustomerDTO
        CreateMap<Customer, CustomerDTO>()
            .ForMember(dest => dest.ActiveJobs, opt => opt.MapFrom(src =>
                src.Jobs.Count(j => j.Status == JobStatus.Active)))
            .ForMember(dest => dest.CompletedJobs, opt => opt.MapFrom(src =>
                src.Jobs.Count(j => j.Status == JobStatus.Completed)))
            .ForMember(dest => dest.TotalRevenue, opt => opt.MapFrom(src =>
                src.Jobs.Where(j => j.Status == JobStatus.Completed).Sum(j => j.Price)));

        // Customer Entity -> CustomerSummaryDTO
        CreateMap<Customer, CustomerSummaryDTO>()
            .ForMember(dest => dest.TotalJobs, opt => opt.MapFrom(src => src.Jobs.Count))
            .ForMember(dest => dest.TotalRevenue, opt => opt.MapFrom(src =>
                src.Jobs.Where(j => j.Status == JobStatus.Completed).Sum(j => j.Price)));

        // CreateCustomerDTO -> Customer Entity
        CreateMap<CreateCustomerDTO, Customer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Jobs, opt => opt.Ignore())
            .ForMember(dest => dest.Invoices, opt => opt.Ignore());

        // UpdateCustomerDTO -> Customer Entity
        CreateMap<UpdateCustomerDTO, Customer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Jobs, opt => opt.Ignore())
            .ForMember(dest => dest.Invoices, opt => opt.Ignore());
    }

    private void CreateJobMappings()
    {
        // Job Entity -> JobDTO
        CreateMap<Job, JobDTO>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.CustomerIsDeleted, opt => opt.MapFrom(src => src.Customer.IsDeleted))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer.Phone))
            .ForMember(dest => dest.CustomerCreatedDate, opt => opt.MapFrom(src => src.Customer.CreatedDate))
            .ForMember(dest => dest.CustomerUpdatedDate, opt => opt.MapFrom(src => src.Customer.UpdatedDate));

        // Job Entity -> JobSummaryDTO
        CreateMap<Job, JobSummaryDTO>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name));

        // CreateJobDTO -> Job Entity
        CreateMap<CreateJobDTO, Job>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.IsInvoiced, opt => opt.Ignore())
            .ForMember(dest => dest.InvoiceId, opt => opt.Ignore())
            .ForMember(dest => dest.InvoicedDate, opt => opt.Ignore())
            .ForMember(dest => dest.Customer, opt => opt.Ignore())
            .ForMember(dest => dest.Invoice, opt => opt.Ignore());

        // UpdateJobDTO -> Job Entity
        CreateMap<UpdateJobDTO, Job>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.IsInvoiced, opt => opt.Ignore())
            .ForMember(dest => dest.InvoiceId, opt => opt.Ignore())
            .ForMember(dest => dest.InvoicedDate, opt => opt.Ignore())
            .ForMember(dest => dest.Customer, opt => opt.Ignore())
            .ForMember(dest => dest.Invoice, opt => opt.Ignore());
    }

    private void CreateInvoiceMappings()
    {
        // Invoice Entity -> InvoiceDTO
        CreateMap<Invoice, InvoiceDTO>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer.Phone))
            .ForMember(dest => dest.CustomerIsDeleted, opt => opt.MapFrom(src => src.Customer.IsDeleted))
            .ForMember(dest => dest.CustomerCreatedDate, opt => opt.MapFrom(src => src.Customer.CreatedDate))
            .ForMember(dest => dest.CustomerUpdatedDate, opt => opt.MapFrom(src => src.Customer.UpdatedDate))
            .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src =>
                src.LineItems.Where(li => li.Job != null && !string.IsNullOrWhiteSpace(li.Job.Address))
                    .Select(li => li.Job.Address).Distinct().ToList()))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src =>
                src.LineItems.Where(li => li.Job != null && !string.IsNullOrWhiteSpace(li.Job.Address))
                    .Select(li => li.Job.Address).Distinct().FirstOrDefault() ?? ""))
            .ForMember(dest => dest.LineItems, opt => opt.MapFrom(src => src.LineItems))
            .ForMember(dest => dest.HasReceipt, 
                opt => opt.MapFrom(src => src.ReceiptInvoices.Any(ri => !ri.Receipt.IsDeleted)));

        // Invoice Entity -> InvoiceSummaryDTO
        CreateMap<Invoice, InvoiceSummaryDTO>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src =>
                src.Status != InvoiceStatus.Paid &&
                src.Status != InvoiceStatus.Cancelled &&
                src.DueDate.HasValue &&
                DateTime.Today > src.DueDate.Value))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => 
                src.LineItems.Where(li => li.Job != null && !string.IsNullOrWhiteSpace(li.Job.Address))
                    .Select(li => li.Job.Address).Distinct().FirstOrDefault() ?? ""));

        // InvoiceLineItem Entity -> InvoiceLineItemDTO
        CreateMap<InvoiceLineItem, InvoiceLineItemDTO>()
            .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.Job.Title))
            .ForMember(dest => dest.JobType, opt => opt.MapFrom(src => src.Job.Type))
            .ForMember(dest => dest.JobAddress, opt => opt.MapFrom(src => src.Job.Address));

        // CreateInvoiceDTO -> Invoice Entity (will be handled in service layer)
        CreateMap<CreateInvoiceDTO, Invoice>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.InvoiceNumber, opt => opt.Ignore())
            .ForMember(dest => dest.Subtotal, opt => opt.Ignore())
            .ForMember(dest => dest.VatAmount, opt => opt.Ignore())
            .ForMember(dest => dest.Total, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => InvoiceStatus.Draft))
            .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentReference, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.SentDate, opt => opt.Ignore())
            .ForMember(dest => dest.DueDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Customer, opt => opt.Ignore())
            .ForMember(dest => dest.LineItems, opt => opt.Ignore());
    }

    private void CreateUserMappings()
    {
        // User Entity -> UserDTO
        CreateMap<User, UserDTO>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()));
    }

    private void CreateReceiptMappings()
    {
        // Receipt Entity -> ReceiptDTO
        CreateMap<Receipt, ReceiptDTO>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.CustomerIsDeleted, opt => opt.MapFrom(src => src.Customer.IsDeleted)) 
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer.Phone))
            .ForMember(dest => dest.Invoices, opt => opt.MapFrom(src => src.ReceiptInvoices));

        // Receipt Entity -> ReceiptSummaryDTO
        CreateMap<Receipt, ReceiptSummaryDTO>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.InvoiceCount, opt => opt.MapFrom(src => src.ReceiptInvoices.Count));

        // ReceiptInvoice Entity -> ReceiptInvoiceDTO
        CreateMap<ReceiptInvoice, ReceiptInvoiceDTO>()
            .ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src => src.Invoice.InvoiceNumber))
            .ForMember(dest => dest.InvoiceDate, opt => opt.MapFrom(src => src.Invoice.CreatedDate));
    }
}

/// <summary>
/// Mapping profile for dashboard data
/// </summary>
public class DashboardMappingProfile : Profile
{
    public DashboardMappingProfile()
    {
        // Custom mappings for dashboard aggregations will be handled in service layer
        // since they require complex database queries and calculations
    }
}