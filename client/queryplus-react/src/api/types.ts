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
    connectionName: string;
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
    connectionName: string;
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
    connectionName: string;
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

// The API never registers a JsonStringEnumConverter, so job enums always serialize as their
// numeric value - see the numeric-literal-union-plus-label-map pattern above (ExportStatusValue).
export type JobType = 1 | 2;
export const JOB_TYPE_LABELS: Record<JobType, string> = {
    1: "JobType_Shell",
    2: "JobType_Python",
};

export type JobApprovalStatus = 1 | 2 | 3 | 4;
export const JOB_APPROVAL_STATUS_LABELS: Record<JobApprovalStatus, string> = {
    1: "JobApprovalStatus_Draft",
    2: "JobApprovalStatus_PendingApproval",
    3: "JobApprovalStatus_Approved",
    4: "JobApprovalStatus_Rejected",
};
export const JOB_APPROVAL_STATUS_BADGE: Record<
    JobApprovalStatus,
    "success" | "neutral" | "danger" | "warning"
> = {
    1: "neutral",
    2: "warning",
    3: "success",
    4: "danger",
};

export type JobRunStatus = 1 | 2 | 3 | 4 | 5 | 6 | 7;
export const JOB_RUN_STATUS_LABELS: Record<JobRunStatus, string> = {
    1: "JobRunStatus_Queued",
    2: "JobRunStatus_Starting",
    3: "JobRunStatus_Running",
    4: "JobRunStatus_Succeeded",
    5: "JobRunStatus_Failed",
    6: "JobRunStatus_Lost",
    7: "JobRunStatus_MissedTrigger",
};
export const JOB_RUN_STATUS_BADGE: Record<
    JobRunStatus,
    "success" | "neutral" | "danger" | "warning"
> = {
    1: "neutral",
    2: "neutral",
    3: "neutral",
    4: "success",
    5: "danger",
    6: "danger",
    7: "warning",
};

/** Queued/Starting/Running are the only non-terminal states - everything else is a final outcome. */
export function isTerminalJobRunStatus(status: JobRunStatus): boolean {
    return status === 4 || status === 5 || status === 6 || status === 7;
}

export type JobTriggerSource = 1 | 2;
export const JOB_TRIGGER_SOURCE_LABELS: Record<JobTriggerSource, string> = {
    1: "TriggeredBy_Schedule",
    2: "TriggeredBy_Manual",
};

/** Route-literal segment for GET /api/jobs/runs/{id}/logs/{stream} - not a numeric DTO enum. */
export type JobLogStream = "Stdout" | "Stderr";

export interface JobListItem {
    id: number;
    name: string;
    jobType: JobType;
    scriptPath?: string | null;
    approvalStatus: JobApprovalStatus;
    enabled: boolean;
    cronExpression: string;
    runAsUser: string;
    createdBy: string;
    approvedBy: string | null;
    updatedAt: string | null;
}

export interface JobDetail {
    id: number;
    name: string;
    description: string | null;
    jobType: JobType;
    scriptPath: string | null;
    scriptSha256: string | null;
    cronExpression: string;
    runAsUser: string;
    memoryLimitMb: number;
    maxDurationMinutes: number;
    enabled: boolean;
    approvalStatus: JobApprovalStatus;
    createdBy: string;
    approvedBy: string | null;
    approvedAt: string | null;
    rejectionReason: string | null;
    notifyEmails: string | null;
    createdAt: string;
    updatedAt: string | null;
}

/** Mirrors CreateJobDefinitionDto/UpdateJobDefinitionDto - deliberately no enabled/approvalStatus/scriptSha256/scriptPath (server-managed via the script upload endpoint). */
export interface JobInput {
    id?: number;
    name: string;
    description: string | null;
    jobType: JobType;
    cronExpression: string;
    runAsUser: string;
    memoryLimitMb: number;
    maxDurationMinutes: number;
    notifyEmails: string | null;
}

export interface RejectJobRequest {
    reason: string;
}

export interface ApproveJobRequest {
    comment?: string | null;
}

export interface JobRunListItem {
    id: number;
    jobDefinitionId: number;
    status: JobRunStatus;
    triggeredBy: JobTriggerSource;
    startedAt: string | null;
    finishedAt: string | null;
    exitCode: number | null;
    hostMachine: string | null;
}

export interface JobRunDetail {
    id: number;
    jobDefinitionId: number;
    status: JobRunStatus;
    triggeredBy: JobTriggerSource;
    runnerPid: number | null;
    runnerStartedAtUtc: string | null;
    childPid: number | null;
    childStartedAtUtc: string | null;
    lastHeartbeatUtc: string | null;
    startedAt: string | null;
    finishedAt: string | null;
    exitCode: number | null;
    stdoutPath: string | null;
    stderrPath: string | null;
    hostMachine: string | null;
    createdAt: string;
}

export interface JobRunRequest {
    id: number;
    jobDefinitionId: number;
    requestedBy: string;
    requestedAt: string;
    consumedAt: string | null;
    jobRunId: number | null;
}

export interface ExecutionLog {
    id: number;
    procedureId: number;
    procedureCaption: string;
    connectionName: string;
    username: string;
    ipAddress: string | null;
    executionStart: string;
    executionEnd: string | null;
    success: boolean;
    errorMessage: string | null;
    rowCount: number | null;
}
