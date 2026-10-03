namespace LibraryTemplate.Domain;

public enum AgeRestriction
{
    General = 0,
    AdultOnly = 1
}

public enum BorrowerCategory
{
    Adult = 0,
    Child = 1
}

public enum LoanStatus
{
    Active = 0,
    Returned = 1
}

public enum UserRole
{
    Borrower = 0,
    LibraryAdmin = 1,
    SystemAdmin = 2
}
