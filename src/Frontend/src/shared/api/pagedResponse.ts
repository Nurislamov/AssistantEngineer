export interface PagedResponse<TItem> {
  items: TItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  search?: string | null;
  sortBy?: string | null;
  sortDescending: boolean;
}
