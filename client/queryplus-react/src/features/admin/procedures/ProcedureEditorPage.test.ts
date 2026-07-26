import { procedureFormToApi, type ProcedureFormValues } from "./ProcedureEditorPage";

const form: ProcedureFormValues = {
    categoryId: 3,
    caption: "  Report ",
    databaseName: " Main ",
    procedureName: " dbo.Report ",
    roleEntitlement: " analyst ",
    enabled: true,
    supportsPagination: false,
    description: " ",
    parameters: [
        {
            caption: " Status ",
            name: " @Status ",
            parameterType: 6,
            defaultValue: " A ",
            comboValues: '["A", "B"]',
            isRequired: true,
        },
    ],
    columns: [
        {
            technicalName: " Code ",
            caption: " Code ",
            alignment: 2,
            formatMask: " ",
            visible: true,
        },
    ],
};

test("procedure form transforms local arrays and canonicalizes combo JSON", () => {
    const result = procedureFormToApi(form, 9);
    expect(result).toMatchObject({
        id: 9,
        caption: "Report",
        databaseName: "Main",
        description: null,
    });
    expect(result.parameters[0]).toMatchObject({ name: "@Status", comboValues: '["A","B"]' });
    expect(result.columns[0]).toMatchObject({ technicalName: "Code", formatMask: null });
});
