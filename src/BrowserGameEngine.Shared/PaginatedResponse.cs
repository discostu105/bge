using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.Shared;

public record PaginatedResponse<T>(
	List<T> Items,
	int Page,
	int PageSize,
	int TotalCount,
	int TotalPages
)
{
	public static PaginatedResponse<T> Create(IEnumerable<T> source, int page, int pageSize)
	{
		page = Math.Max(1, page);
		pageSize = Math.Clamp(pageSize, 1, 100);

		var items = source.ToList();
		var totalCount = items.Count;
		var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

		var paged = items
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToList();

		return new PaginatedResponse<T>(paged, page, pageSize, totalCount, totalPages);
	}
}
