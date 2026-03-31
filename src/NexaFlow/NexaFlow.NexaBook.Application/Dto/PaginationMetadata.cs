namespace NexaFlow.NexaBook.Application.Dto
{
    public record PaginationMetadata(int CurrentPage, int PageSize, int TotalCount)
    {
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNext => CurrentPage < TotalPages;
        public bool HasPrev => CurrentPage > 1;
    }
}
