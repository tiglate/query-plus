export function compareSortValues(av: unknown, bv: unknown, asc: boolean): number {
  const a = String(av ?? "")
    .replace(/<[^>]+>/g, " ")
    .trim();
  const b = String(bv ?? "")
    .replace(/<[^>]+>/g, " ")
    .trim();
  const an = Number(a.replace(",", "."));
  const bn = Number(b.replace(",", "."));
  if (!Number.isNaN(an) && !Number.isNaN(bn) && a !== "" && b !== "") {
    return asc ? an - bn : bn - an;
  }
  return asc ? a.localeCompare(b) : b.localeCompare(a);
}

/**
 * Reorder columns/cells and adjust sortCol when a column is moved.
 * Returns the next sortCol (may be null).
 */
export function applyColumnReorder(
  columns: unknown[],
  cells: unknown[][],
  fromIndex: number,
  toIndex: number,
  sortCol: number | null,
): number | null {
  if (fromIndex === toIndex || fromIndex < 0 || toIndex < 0) return sortCol;
  if (fromIndex >= columns.length || toIndex >= columns.length) return sortCol;

  const [col] = columns.splice(fromIndex, 1);
  columns.splice(toIndex, 0, col);

  for (let r = 0; r < cells.length; r++) {
    const row = cells[r];
    const [val] = row.splice(fromIndex, 1);
    row.splice(toIndex, 0, val);
  }

  if (sortCol === fromIndex) {
    return toIndex;
  }
  if (sortCol !== null) {
    if (fromIndex < sortCol && toIndex >= sortCol) return sortCol - 1;
    if (fromIndex > sortCol && toIndex <= sortCol) return sortCol + 1;
  }
  return sortCol;
}
