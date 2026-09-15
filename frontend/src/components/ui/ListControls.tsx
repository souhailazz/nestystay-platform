import { ChevronLeft, ChevronRight, Download, Search, X } from "lucide-react";
import { cx } from "../../lib/ui";

export type ListSortOption = { value: string; label: string };

type ListControlsProps = {
  label?: string;
  query: string;
  onQueryChange: (value: string) => void;
  sort?: string;
  onSortChange?: (value: string) => void;
  sortOptions?: ListSortOption[];
  total: number;
  page: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onExport?: () => void;
  exportLabel?: string;
  selectable?: boolean;
  allVisibleSelected?: boolean;
  onToggleAllVisible?: () => void;
  className?: string;
};

export function ListControls({
  label = "Filter list",
  query,
  onQueryChange,
  sort,
  onSortChange,
  sortOptions = [],
  total,
  page,
  pageSize,
  onPageChange,
  onExport,
  exportLabel = "Export",
  selectable = false,
  allVisibleSelected = false,
  onToggleAllVisible,
  className,
}: ListControlsProps) {
  const pageCount = Math.max(1, Math.ceil(total / pageSize));
  const start = total === 0 ? 0 : page * pageSize + 1;
  const end = Math.min(total, (page + 1) * pageSize);

  return (
    <div className={cx("grid gap-3 rounded-card border border-sand-border bg-cream p-3", className)}>
      <div className="flex flex-wrap items-center gap-2">
        {selectable && onToggleAllVisible && <label className="inline-flex min-h-11 items-center gap-2 rounded-field border-[1.5px] border-sand-input bg-white px-3 text-xs font-semibold text-ink"><input aria-label="Select all visible rows" checked={allVisibleSelected} onChange={onToggleAllVisible} type="checkbox" /> Select page</label>}
        <label className="flex min-h-11 min-w-[min(100%,300px)] flex-1 items-center gap-2 rounded-field border-[1.5px] border-sand-input bg-white px-3 transition-colors focus-within:border-deep-hover">
          <Search aria-hidden="true" className="shrink-0 text-sand-500" size={16} />
          <span className="sr-only">{label}</span>
          <input aria-label={label} className="min-h-10 min-w-0 flex-1 border-none bg-transparent text-sm text-ink outline-none placeholder:text-sand-500" onChange={(event) => onQueryChange(event.target.value)} placeholder={label} type="search" value={query} />
          {query && <button aria-label="Clear filter" className="grid size-8 place-items-center rounded-pill text-sand-600 hover:bg-shell hover:text-ink" onClick={() => onQueryChange("")} type="button"><X size={15} /></button>}
        </label>
        {sortOptions.length > 0 && onSortChange && <label className="flex min-h-11 items-center gap-2 text-xs font-semibold text-sand-600"><span>Sort</span><select aria-label="Sort list" className="min-h-11 rounded-field border-[1.5px] border-sand-input bg-white px-3 text-sm font-normal text-ink outline-none focus:border-deep-hover" onChange={(event) => onSortChange(event.target.value)} value={sort}><option value="">Default</option>{sortOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></label>}
        {onExport && <button aria-label={exportLabel} className="inline-flex min-h-11 items-center gap-2 rounded-field border-[1.5px] border-sand-input bg-white px-4 text-sm font-semibold text-deep-hover transition-colors hover:border-deep-hover hover:bg-shell" onClick={onExport} type="button"><Download aria-hidden="true" size={15} /> {exportLabel}</button>}
      </div>
      <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-sand-600" aria-live="polite">
        <span>{total === 0 ? "No results" : `Showing ${start}–${end} of ${total}`}</span>
        {pageCount > 1 && <div className="flex items-center gap-1"><button aria-label="Previous page" className="grid size-9 place-items-center rounded-pill border border-sand-input bg-white text-deep-hover disabled:cursor-not-allowed disabled:opacity-40" disabled={page === 0} onClick={() => onPageChange(Math.max(0, page - 1))} type="button"><ChevronLeft size={15} /></button><span className="min-w-16 text-center font-semibold">Page {page + 1} of {pageCount}</span><button aria-label="Next page" className="grid size-9 place-items-center rounded-pill border border-sand-input bg-white text-deep-hover disabled:cursor-not-allowed disabled:opacity-40" disabled={page >= pageCount - 1} onClick={() => onPageChange(Math.min(pageCount - 1, page + 1))} type="button"><ChevronRight size={15} /></button></div>}
      </div>
    </div>
  );
}

export function downloadCsv(fileName: string, headers: string[], rows: Array<Array<string | number | null | undefined>>) {
  const escape = (value: string | number | null | undefined) => `"${String(value ?? "").replaceAll('"', '""')}"`;
  const csv = [headers, ...rows].map((row) => row.map(escape).join(",")).join("\n");
  const url = URL.createObjectURL(new Blob([csv], { type: "text/csv;charset=utf-8" }));
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  window.setTimeout(() => URL.revokeObjectURL(url), 0);
}
