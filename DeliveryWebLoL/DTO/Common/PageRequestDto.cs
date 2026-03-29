namespace DeliveryWebLoL.DTO.Common
{
    public class PageRequestDto
    {
        // 1-based page number
        public int Page { get; set; } = 1;

        // page size / limit
        public int PageSize { get; set; } = 10;

        // optional free-text search
        public string? Search { get; set; }

        public int Skip => (Page <= 1 ? 0 : (Page - 1) * PageSize);

        public int Take => PageSize <= 0 ? 10 : PageSize;
    }
}
