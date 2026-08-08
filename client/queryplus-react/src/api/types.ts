export type ParameterType = 0 | 1 | 2 | 3 | 4 | 5 | 6;
export type ColumnAlignment = 0 | 1 | 2;

export interface AuthUser {
    username: string | null;
    roles: string[];
    isAuthenticated: boolean;
}

export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}

export interface AuditFields {
    createdAt: string;
    updatedAt: string | null;
    createdBy?: string | null;
    updatedBy?: string | null;
}

export interface CategoryListItem extends AuditFields {
    id: number;
    description: string;
}

export type CategoryDetail = CategoryListItem;
export interface CategoryInput {
    id?: number;
    description: string;
}

export interface ProcedureLookup {
    id: number;
    categoryId: number;
    categoryDescription: string | null;
    caption: string;
    description: string | null;
    roleEntitlement: string;
    supportsPagination: boolean;
}

export interface ProcedureListItem extends AuditFields {
    id: number;
    categoryId: number;
    categoryDescription: string | null;
    caption: string;
    databaseName: string;
    procedureName: string;
    enabled: boolean;
    supportsPagination: boolean;
    roleEntitlement: string;
}

export interface ProcedureParameter {
    id?: number;
    caption: string;
    name: string;
    parameterType: ParameterType;
    defaultValue: string | null;
    comboValues: string | null;
    comboOptions?: string[];
    isRequired: boolean;
}

export interface ProcedureColumn {
    id?: number;
    technicalName: string;
    caption: string;
    alignment: ColumnAlignment;
    formatMask: string | null;
    visible: boolean;
}

export interface ProcedureDetail extends ProcedureListItem {
    description: string | null;
    createdBy: string | null;
    updatedBy: string | null;
    parameters: ProcedureParameter[];
    columns: ProcedureColumn[];
}

export interface ProcedureInput {
    id?: number;
    categoryId: number;
    caption: string;
    databaseName: string;
    procedureName: string;
    enabled: boolean;
    supportsPagination: boolean;
    roleEntitlement: string;
    description: string | null;
    parameters: ProcedureParameter[];
    columns: ProcedureColumn[];
}

export interface SyncMetadataRequest {
    databaseName: string;
    procedureName: string;
}

export interface ExecuteRequest {
    procedureId: number;
    parameterValues: Record<string, string | null>;
    pageNumber: number;
    pageSize: number;
}

export interface GridColumn {
    technicalName: string;
    caption: string;
    alignment: ColumnAlignment;
    formatMask: string | null;
    visible: boolean;
}

export type GridCell = string | number | boolean | null;
export interface ExecuteResponse {
    success: boolean;
    errorMessage?: string | null;
    executionLogId?: number | null;
    procedureId: number;
    procedureCaption?: string | null;
    rowCount: number;
    supportsPagination: boolean;
    pageNumber: number;
    pageSize: number;
    totalRecords: number | null;
    columns: GridColumn[];
    rows: GridCell[][];
}

export interface ExportRequest {
    procedureId: number;
    parameterValues: Record<string, string | null>;
}

// The API never registers a JsonStringEnumConverter, so ExportJobStatus always serializes as
// its numeric value (0=Queued, 1=Running, 2=Completed, 3=Failed) - see normalizeExportStatus.
export type ExportStatusValue = 0 | 1 | 2 | 3;
export interface ExportJob {
    id: string;
    status: ExportStatusValue;
    fileName?: string | null;
    errorMessage?: string | null;
    rowCount?: number | null;
    createdAt?: string;
    completedAt?: string | null;
    username?: string | null;
}

export interface ExecutionLog {
    id: number;
    procedureId: number;
    procedureCaption: string;
    username: string;
    ipAddress: string | null;
    executionStart: string;
    executionEnd: string | null;
    success: boolean;
    errorMessage: string | null;
    rowCount: number | null;
}
