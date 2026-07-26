import { categoryFormToApi } from "./CategoriesPage";

test("category form trims values and preserves id", () => {
    expect(categoryFormToApi({ description: "  Finance  " }, 4)).toEqual({
        id: 4,
        description: "Finance",
    });
});
