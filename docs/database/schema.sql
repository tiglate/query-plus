-- =============================================
-- Query Plus - Database Schema (INT version)
-- =============================================

IF OBJECT_ID('tb_revision', 'U') IS NULL
BEGIN
    CREATE TABLE tb_revision (
        id_revision INT NOT NULL IDENTITY(1, 1),
        revision_timestamp DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
        username VARCHAR(100) NOT NULL,
        ip_address VARCHAR(45) NULL,
        CONSTRAINT pk_revision PRIMARY KEY CLUSTERED (id_revision)
    );
END

IF OBJECT_ID('tb_revision_type', 'U') IS NULL
BEGIN
    CREATE TABLE tb_revision_type (
        id_revision_type TINYINT NOT NULL,
        description VARCHAR(50) NOT NULL,
        CONSTRAINT pk_revision_type PRIMARY KEY (id_revision_type)
    );

    INSERT INTO tb_revision_type (id_revision_type, description)
    VALUES 
        (1, 'INSERT'),
        (2, 'UPDATE'),
        (3, 'DELETE');
END

-- =============================================
-- Categories
-- =============================================
IF OBJECT_ID('tb_category', 'U') IS NULL
BEGIN
    CREATE TABLE tb_category (
        id_category INT NOT NULL IDENTITY(1, 1),
        description VARCHAR(200) NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
        updated_at DATETIME2 NULL,
        CONSTRAINT pk_category PRIMARY KEY CLUSTERED (id_category),
        CONSTRAINT uq_category_description UNIQUE (description)
    );

    CREATE TABLE tb_category_aud (
        id_category INT NOT NULL,
        id_revision INT NOT NULL,
        id_revision_type TINYINT NULL,
        description VARCHAR(200) NULL,
        created_at DATETIME2 NULL,
        updated_at DATETIME2 NULL,
        CONSTRAINT pk_category_aud PRIMARY KEY (id_category, id_revision),
        CONSTRAINT fk_category_aud_revision FOREIGN KEY (id_revision) REFERENCES tb_revision (id_revision),
        CONSTRAINT fk_category_aud_revision_type FOREIGN KEY (id_revision_type) REFERENCES tb_revision_type (id_revision_type)
    );
END

-- =============================================
-- Procedures
-- =============================================
IF OBJECT_ID('tb_procedure', 'U') IS NULL
BEGIN
    CREATE TABLE tb_procedure (
        id_procedure INT NOT NULL IDENTITY(1, 1),
        id_category INT NOT NULL,
        caption VARCHAR(300) NOT NULL,
        database_name VARCHAR(128) NOT NULL,
        procedure_name VARCHAR(128) NOT NULL,
        enabled BIT NOT NULL DEFAULT(1),
        role_entitlement VARCHAR(100) NOT NULL,
        description VARCHAR(500) NULL,
        created_at DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
        updated_at DATETIME2 NULL,
        CONSTRAINT pk_procedure PRIMARY KEY CLUSTERED (id_procedure),
        CONSTRAINT fk_procedure_category FOREIGN KEY (id_category) REFERENCES tb_category (id_category),
        CONSTRAINT uq_procedure_caption UNIQUE (caption),
        CONSTRAINT uq_procedure_db_proc UNIQUE (database_name, procedure_name)
    );

    CREATE TABLE tb_procedure_aud (
        id_procedure INT NOT NULL,
        id_revision INT NOT NULL,
        id_revision_type TINYINT NULL,
        id_category INT NULL,
        caption VARCHAR(300) NULL,
        database_name VARCHAR(128) NULL,
        procedure_name VARCHAR(128) NULL,
        enabled BIT NULL,
        role_entitlement VARCHAR(100) NULL,
        description VARCHAR(500) NULL,
        created_at DATETIME2 NULL,
        updated_at DATETIME2 NULL,
        CONSTRAINT pk_procedure_aud PRIMARY KEY (id_procedure, id_revision),
        CONSTRAINT fk_procedure_aud_revision FOREIGN KEY (id_revision) REFERENCES tb_revision (id_revision),
        CONSTRAINT fk_procedure_aud_revision_type FOREIGN KEY (id_revision_type) REFERENCES tb_revision_type (id_revision_type)
    );
END

-- =============================================
-- Parameters & Columns
-- =============================================
IF OBJECT_ID('tb_procedure_parameter', 'U') IS NULL
BEGIN
    CREATE TABLE tb_procedure_parameter (
        id_procedure_parameter INT NOT NULL IDENTITY(1, 1),
        id_procedure INT NOT NULL,
        caption VARCHAR(200) NOT NULL,
        name VARCHAR(128) NOT NULL,
        parameter_type VARCHAR(50) NOT NULL,
        default_value NVARCHAR(500) NULL,
        combo_values NVARCHAR(MAX) NULL,           -- JSON array
        is_required BIT NOT NULL DEFAULT(0),
        created_at DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
        updated_at DATETIME2 NULL,
        CONSTRAINT pk_procedure_parameter PRIMARY KEY CLUSTERED (id_procedure_parameter),
        CONSTRAINT fk_parameter_procedure FOREIGN KEY (id_procedure) REFERENCES tb_procedure (id_procedure) ON DELETE CASCADE,
        CONSTRAINT uq_parameter_procedure_name UNIQUE (id_procedure, name)
    );

    CREATE TABLE tb_procedure_parameter_aud (
        id_procedure_parameter INT NOT NULL,
        id_revision INT NOT NULL,
        id_revision_type TINYINT NULL,
        id_procedure INT NULL,
        caption VARCHAR(200) NULL,
        name VARCHAR(128) NULL,
        parameter_type VARCHAR(50) NULL,
        default_value NVARCHAR(500) NULL,
        combo_values NVARCHAR(MAX) NULL,
        is_required BIT NULL,
        created_at DATETIME2 NULL,
        updated_at DATETIME2 NULL,
        CONSTRAINT pk_parameter_aud PRIMARY KEY (id_procedure_parameter, id_revision),
        CONSTRAINT fk_parameter_aud_revision FOREIGN KEY (id_revision) REFERENCES tb_revision (id_revision),
        CONSTRAINT fk_parameter_aud_revision_type FOREIGN KEY (id_revision_type) REFERENCES tb_revision_type (id_revision_type)
    );
END

IF OBJECT_ID('tb_procedure_column', 'U') IS NULL
BEGIN
    CREATE TABLE tb_procedure_column (
        id_procedure_column INT NOT NULL IDENTITY(1, 1),
        id_procedure INT NOT NULL,
        technical_name VARCHAR(128) NOT NULL,
        caption VARCHAR(200) NOT NULL,
        alignment VARCHAR(10) NOT NULL DEFAULT('Left'),
        format_mask VARCHAR(100) NULL,
        visible BIT NOT NULL DEFAULT(1),
        created_at DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
        updated_at DATETIME2 NULL,
        CONSTRAINT pk_procedure_column PRIMARY KEY CLUSTERED (id_procedure_column),
        CONSTRAINT fk_column_procedure FOREIGN KEY (id_procedure) REFERENCES tb_procedure (id_procedure) ON DELETE CASCADE,
        CONSTRAINT uq_column_procedure_tech UNIQUE (id_procedure, technical_name)
    );

    CREATE TABLE tb_procedure_column_aud (
        id_procedure_column INT NOT NULL,
        id_revision INT NOT NULL,
        id_revision_type TINYINT NULL,
        id_procedure INT NULL,
        technical_name VARCHAR(128) NULL,
        caption VARCHAR(200) NULL,
        alignment VARCHAR(10) NULL,
        format_mask VARCHAR(100) NULL,
        visible BIT NULL,
        created_at DATETIME2 NULL,
        updated_at DATETIME2 NULL,
        CONSTRAINT pk_column_aud PRIMARY KEY (id_procedure_column, id_revision),
        CONSTRAINT fk_column_aud_revision FOREIGN KEY (id_revision) REFERENCES tb_revision (id_revision),
        CONSTRAINT fk_column_aud_revision_type FOREIGN KEY (id_revision_type) REFERENCES tb_revision_type (id_revision_type)
    );
END

-- =============================================
-- Execution Log
-- =============================================
IF OBJECT_ID('tb_execution_log', 'U') IS NULL
BEGIN
    CREATE TABLE tb_execution_log (
        id_execution_log INT NOT NULL IDENTITY(1, 1),
        id_procedure INT NOT NULL,
        username VARCHAR(100) NOT NULL,
        ip_address VARCHAR(45) NULL,
        execution_start DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
        execution_end DATETIME2 NULL,
        success BIT NOT NULL DEFAULT(1),
        error_message NVARCHAR(MAX) NULL,
        parameter_values NVARCHAR(MAX) NULL,        -- JSON
        row_count INT NULL,
        CONSTRAINT pk_execution_log PRIMARY KEY CLUSTERED (id_execution_log),
        CONSTRAINT fk_log_procedure FOREIGN KEY (id_procedure) REFERENCES tb_procedure (id_procedure)
    );

    CREATE NONCLUSTERED INDEX ix_execution_log_user_date ON tb_execution_log (username, execution_start DESC);
    CREATE NONCLUSTERED INDEX ix_execution_log_proc_date ON tb_execution_log (id_procedure, execution_start DESC);
END
