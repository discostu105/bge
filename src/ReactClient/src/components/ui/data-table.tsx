import type { ReactNode } from 'react'
import {
  type ColumnDef,
  type SortingState,
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  useReactTable,
} from '@tanstack/react-table'
import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import { useState } from 'react'
import { cn } from '@/lib/utils'
import { Skeleton } from '@/components/ui/skeleton'

export type { ColumnDef }

interface DataTableProps<TRow> {
  columns: ColumnDef<TRow, unknown>[]
  rows: TRow[] | undefined
  loading?: boolean
  error?: ReactNode
  empty?: ReactNode
  sortable?: boolean
  stickyHeader?: boolean
  density?: 'compact' | 'comfortable'
  onRowClick?: (row: TRow) => void
  getRowId?: (row: TRow, index: number) => string
  className?: string
}

export function DataTable<TRow>({
  columns, rows, loading, error, empty,
  sortable = false, stickyHeader = false, density = 'comfortable', onRowClick, getRowId, className,
}: DataTableProps<TRow>) {
  const [sorting, setSorting] = useState<SortingState>([])
  const table = useReactTable({
    data: rows ?? [],
    columns,
    state: sortable ? { sorting } : undefined,
    onSortingChange: sortable ? setSorting : undefined,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: sortable ? getSortedRowModel() : undefined,
    getRowId: getRowId ? (r, i) => getRowId(r, i) : undefined,
  })

  const cellPad = density === 'compact' ? 'py-1.5 px-3' : 'py-2.5 px-3'

  return (
    <div className={cn('relative overflow-x-auto rounded-md border bg-card', className)}>
      <table className="w-full text-sm">
        <thead className={cn('text-left', stickyHeader && 'sticky top-0 z-10 bg-card')}>
          {table.getHeaderGroups().map((hg) => (
            <tr key={hg.id} className="border-b">
              {hg.headers.map((h) => {
                const canSort = sortable && h.column.getCanSort()
                const sortDir = h.column.getIsSorted()
                return (
                  <th
                    key={h.id}
                    scope="col"
                    className={cn('label font-semibold', cellPad, canSort && 'cursor-pointer select-none')}
                    onClick={canSort ? h.column.getToggleSortingHandler() : undefined}
                    aria-sort={sortDir === 'asc' ? 'ascending' : sortDir === 'desc' ? 'descending' : 'none'}
                  >
                    <span className="inline-flex items-center gap-1">
                      {flexRender(h.column.columnDef.header, h.getContext())}
                      {canSort && (
                        sortDir === 'asc' ? <ArrowUp className="h-3 w-3" aria-hidden /> :
                        sortDir === 'desc' ? <ArrowDown className="h-3 w-3" aria-hidden /> :
                        <ChevronsUpDown className="h-3 w-3 opacity-50" aria-hidden />
                      )}
                    </span>
                  </th>
                )
              })}
            </tr>
          ))}
        </thead>
        <tbody>
          {loading && Array.from({ length: 4 }).map((_, i) => (
            <tr key={`skel-${i}`} className="border-b">
              {columns.map((_, j) => (
                <td key={j} className={cellPad}><Skeleton className="h-4 w-full max-w-40" /></td>
              ))}
            </tr>
          ))}
          {!loading && error && (
            <tr><td colSpan={columns.length} className="px-3 py-8 text-center text-danger">{error}</td></tr>
          )}
          {!loading && !error && table.getRowModel().rows.length === 0 && (
            <tr><td colSpan={columns.length} className="px-3 py-8 text-center text-muted-foreground">{empty ?? 'No results.'}</td></tr>
          )}
          {!loading && !error && table.getRowModel().rows.map((r) => (
            <tr
              key={r.id}
              className={cn('border-b last:border-b-0', onRowClick && 'cursor-pointer hover:bg-muted/40')}
              onClick={onRowClick ? () => onRowClick(r.original) : undefined}
            >
              {r.getVisibleCells().map((c) => (
                <td key={c.id} className={cn(cellPad, 'align-middle')}>
                  {flexRender(c.column.columnDef.cell, c.getContext())}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
