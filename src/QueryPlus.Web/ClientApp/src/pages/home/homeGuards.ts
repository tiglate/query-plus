/**
 * Pure / DOM-light helpers for home execute/export guards (unit-testable).
 */

export function isValidProcedureId(value: string | null | undefined): boolean {
  const v = (value || "").trim();
  return !!v && Number(v) > 0;
}

export function isParamFieldName(name: string | null | undefined): boolean {
  if (!name) return false;
  return name.startsWith("param_") || name.startsWith("paramcheck_");
}

export function formatRequiredParamsMessage(
  missing: string[],
  singleTemplate: string,
  multiTemplate: string,
): string {
  if (missing.length === 0) return "";
  if (missing.length === 1) {
    return singleTemplate.replace("{0}", missing[0]);
  }
  return multiTemplate.replace("{0}", missing.join(", "));
}

export interface RequiredParamField {
  caption: string;
  value: string;
  markInvalid: () => void;
}

/**
 * Given field descriptors, return captions that are empty (required missing).
 */
export function findMissingRequiredCaptions(fields: RequiredParamField[]): string[] {
  const missing: string[] = [];
  for (const field of fields) {
    if (!(field.value || "").trim()) {
      missing.push(field.caption);
      field.markInvalid();
    }
  }
  return missing;
}

export function canExport(hasProcedure: boolean, exportReady: boolean): boolean {
  return hasProcedure && exportReady;
}
