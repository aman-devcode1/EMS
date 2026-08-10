namespace EMS.Core.DTOs.Employee;

public class EmployeeQueryParams
{
    // ----- Pagination ----- 
    private int _pageNumber = 1;
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value; // Ensure page number is at least 1
    }

// ----- Page Size -----
    private int _pageSize = 10;
    private const int MaxPageSize = 50;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value < 1) ? 10 : (value > MaxPageSize ? MaxPageSize : value); // Ensure page size is between 1 and MaxPageSize
    }

    // ----- Search -----
    public string? SearchTerm { get; set; }
    
    // ----- Sorting -----
    public string? SortBy { get; set; }
    private string _sortDirection = "asc";
    public string? SortDirection
    {
        get => _sortDirection;
        set => _sortDirection = (value?.ToLower() == "desc") ? "desc" : "asc"; // Default to "asc" if not "desc"
    }

    // ----- Filtering -----
    public string? Department { get; set; }
    
    public string? Designation { get; set; }

    public bool? IsActive { get; set; }
}