using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Mapping;

/// <summary>
/// Maps SaveProcedureDto graphs onto entities for create/update (master-detail).
/// Kept manual for explicit control over collection sync.
/// </summary>
public static class ProcedureGraphMapper
{
    public static Procedure ToNewEntity(SaveProcedureDto dto)
    {
        var entity = new Procedure
        {
            IdCategory = dto.CategoryId,
            Caption = dto.Caption.Trim(),
            DatabaseName = dto.DatabaseName.Trim(),
            ProcedureName = dto.ProcedureName.Trim(),
            Enabled = dto.Enabled,
            RoleEntitlement = dto.RoleEntitlement.Trim(),
            Description = NormalizeOptional(dto.Description)
        };

        foreach (var p in dto.Parameters)
        {
            entity.Parameters.Add(ToNewParameter(p));
        }

        foreach (var c in dto.Columns)
        {
            entity.Columns.Add(ToNewColumn(c));
        }

        return entity;
    }

    public static void ApplyUpdate(Procedure entity, SaveProcedureDto dto)
    {
        entity.IdCategory = dto.CategoryId;
        entity.Caption = dto.Caption.Trim();
        entity.DatabaseName = dto.DatabaseName.Trim();
        entity.ProcedureName = dto.ProcedureName.Trim();
        entity.Enabled = dto.Enabled;
        entity.RoleEntitlement = dto.RoleEntitlement.Trim();
        entity.Description = NormalizeOptional(dto.Description);

        SyncParameters(entity, dto.Parameters);
        SyncColumns(entity, dto.Columns);
    }

    private static void SyncParameters(Procedure entity, IList<SaveProcedureParameterDto> dtos)
    {
        var incomingIds = dtos.Where(d => d.Id is > 0).Select(d => d.Id!.Value).ToHashSet();
        var toRemove = entity.Parameters.Where(p => !incomingIds.Contains(p.IdProcedureParameter)).ToList();
        foreach (var remove in toRemove)
        {
            entity.Parameters.Remove(remove);
        }

        foreach (var dto in dtos)
        {
            if (dto.Id is > 0)
            {
                var existing = entity.Parameters.FirstOrDefault(p => p.IdProcedureParameter == dto.Id.Value);
                if (existing is null)
                {
                    entity.Parameters.Add(ToNewParameter(dto));
                    continue;
                }

                existing.Caption = dto.Caption.Trim();
                existing.Name = NormalizeParamName(dto.Name);
                existing.ParameterType = dto.ParameterType;
                existing.DefaultValue = NormalizeOptional(dto.DefaultValue);
                existing.ComboValues = NormalizeOptional(dto.ComboValues);
                existing.IsRequired = dto.IsRequired;
            }
            else
            {
                entity.Parameters.Add(ToNewParameter(dto));
            }
        }
    }

    private static void SyncColumns(Procedure entity, IList<SaveProcedureColumnDto> dtos)
    {
        var incomingIds = dtos.Where(d => d.Id is > 0).Select(d => d.Id!.Value).ToHashSet();
        var toRemove = entity.Columns.Where(c => !incomingIds.Contains(c.IdProcedureColumn)).ToList();
        foreach (var remove in toRemove)
        {
            entity.Columns.Remove(remove);
        }

        foreach (var dto in dtos)
        {
            if (dto.Id is > 0)
            {
                var existing = entity.Columns.FirstOrDefault(c => c.IdProcedureColumn == dto.Id.Value);
                if (existing is null)
                {
                    entity.Columns.Add(ToNewColumn(dto));
                    continue;
                }

                existing.TechnicalName = dto.TechnicalName.Trim();
                existing.Caption = dto.Caption.Trim();
                existing.Alignment = dto.Alignment;
                existing.FormatMask = NormalizeOptional(dto.FormatMask);
                existing.Visible = dto.Visible;
            }
            else
            {
                entity.Columns.Add(ToNewColumn(dto));
            }
        }
    }

    private static ProcedureParameter ToNewParameter(SaveProcedureParameterDto dto) => new()
    {
        Caption = dto.Caption.Trim(),
        Name = NormalizeParamName(dto.Name),
        ParameterType = dto.ParameterType,
        DefaultValue = NormalizeOptional(dto.DefaultValue),
        ComboValues = NormalizeOptional(dto.ComboValues),
        IsRequired = dto.IsRequired
    };

    private static ProcedureColumn ToNewColumn(SaveProcedureColumnDto dto) => new()
    {
        TechnicalName = dto.TechnicalName.Trim(),
        Caption = dto.Caption.Trim(),
        Alignment = dto.Alignment,
        FormatMask = NormalizeOptional(dto.FormatMask),
        Visible = dto.Visible
    };

    private static string NormalizeParamName(string name)
    {
        var trimmed = name.Trim();
        return trimmed.StartsWith('@') ? trimmed : $"@{trimmed}";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
