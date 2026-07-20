/**
 * Pure helpers for "Sync metadata" enablement.
 */

export function canSyncMetadata(
  database: string | null | undefined,
  procedure: string | null | undefined,
): boolean {
  return !!(database || "").trim() && !!(procedure || "").trim();
}
