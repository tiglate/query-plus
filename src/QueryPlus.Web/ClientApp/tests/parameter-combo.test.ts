import { afterEach, beforeEach, describe, expect, it } from "vite-plus/test";
import "reflect-metadata";
import { applyComboVisibility, isComboType } from "@/components/parameter-combo/parameterCombo";
import { ParameterComboService } from "@/components/parameter-combo/ParameterComboService";
import { createTestContainer } from "@/core/di/container";

describe("parameterCombo pure", () => {
  it("isComboType matches combo type value", () => {
    expect(isComboType("6", "6")).toBe(true);
    expect(isComboType(6 as unknown as string, "6")).toBe(true);
    expect(isComboType("1", "6")).toBe(false);
  });

  it("applyComboVisibility hides and disables non-combo", () => {
    const type = document.createElement("select");
    type.innerHTML = `<option value="1">Text</option><option value="6">Combo</option>`;
    type.value = "1";
    const combo = document.createElement("input");
    combo.setAttribute("data-combo-type-value", "6");

    expect(applyComboVisibility(type, combo)).toBe(false);
    expect(combo.disabled).toBe(true);
    expect(combo.classList.contains("hidden")).toBe(true);

    type.value = "6";
    expect(applyComboVisibility(type, combo)).toBe(true);
    expect(combo.disabled).toBe(false);
    expect(combo.classList.contains("hidden")).toBe(false);
  });
});

describe("ParameterComboService", () => {
  beforeEach(() => {
    document.body.innerHTML = `
      <table>
        <tr class="js-param-row">
          <td>
            <select class="js-param-type">
              <option value="1" selected>Text</option>
              <option value="6">Combo</option>
            </select>
          </td>
          <td>
            <input class="js-param-combo-values" data-combo-type-value="6" />
          </td>
        </tr>
      </table>
    `;
  });

  afterEach(() => {
    document.body.innerHTML = "";
  });

  it("mounts and reacts to type changes", () => {
    const c = createTestContainer();
    const service = c.resolve(ParameterComboService);
    service.mountAll(document);

    const combo = document.querySelector(".js-param-combo-values") as HTMLInputElement;
    expect(combo.disabled).toBe(true);
    expect(combo.classList.contains("hidden")).toBe(true);

    const type = document.querySelector(".js-param-type") as HTMLSelectElement;
    type.value = "6";
    type.dispatchEvent(new Event("change"));

    expect(combo.disabled).toBe(false);
    expect(combo.classList.contains("hidden")).toBe(false);

    service.dispose();
  });
});
