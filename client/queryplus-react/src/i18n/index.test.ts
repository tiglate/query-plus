import { detectCookieLocale, normalizeLocale } from "./index";

test("culture cookies take compatible locale formats", () => {
    expect(detectCookieLocale("other=x; QueryPlus.Culture=c%3Dpt-BR%7Cuic%3Dpt-BR")).toBe("pt-BR");
    expect(detectCookieLocale(".AspNetCore.Culture=c%3Den%7Cuic%3Den")).toBe("en");
    expect(normalizeLocale("en-US")).toBe("en");
});
