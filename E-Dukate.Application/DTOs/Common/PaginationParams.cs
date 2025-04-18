namespace E_Dukate.Application.DTOs.Common;

public class PaginationParams
{
    private const int MaxPageSize = 10;
    private int _pageSize = MaxPageSize;

    public int PageNumber { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }
}