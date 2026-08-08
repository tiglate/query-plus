import { render, screen } from "@testing-library/react";
import { SupportPage } from "./SupportPage";

test("renders the support title, body copy, and all contact channels", () => {
    render(<SupportPage />);

    expect(screen.getByText("Contact the support team for help with QueryPlus.")).toBeInTheDocument();
    expect(screen.getByText("Helpdesk")).toBeInTheDocument();
    expect(screen.getByText("Phone")).toBeInTheDocument();
    expect(screen.getByText("Email")).toBeInTheDocument();
    expect(screen.getByText("Working hours")).toBeInTheDocument();
    expect(screen.getByText("support@queryplus.local")).toBeInTheDocument();
});
